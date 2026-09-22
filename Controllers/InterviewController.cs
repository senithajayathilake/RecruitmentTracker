using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;
using RecruitmentTracker.Services;

namespace RecruitmentTracker.Controllers;

[Authorize]
public class InterviewController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notifications;

    public InterviewController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        INotificationService notifications)
    {
        _db = db;
        _userManager = userManager;
        _notifications = notifications;
    }

    [Authorize(Roles = "HR,HiringManager")]
    public async Task<IActionResult> Index()
    {
        var interviews = await _db.Interviews
            .Include(i => i.Application)!.ThenInclude(a => a!.Candidate)
            .Include(i => i.Application)!.ThenInclude(a => a!.Vacancy)
            .Include(i => i.InterviewStage)
            .Include(i => i.Interviewer)
            .Include(i => i.Feedback)
            .OrderByDescending(i => i.ScheduledAt)
            .ToListAsync();

        return View(interviews);
    }

    [Authorize(Roles = "HR")]
    [HttpGet]
    public async Task<IActionResult> Schedule(int applicationId)
    {
        var application = await _db.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Vacancy)!.ThenInclude(v => v!.InterviewStages)
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

        if (application?.Vacancy == null) return NotFound();

        if (!application.Vacancy.InterviewStages.Any())
        {
            TempData["Error"] = "Add at least one interview stage to this vacancy before scheduling an interview.";
            return RedirectToAction("Details", "Application", new { id = applicationId });
        }

        await PopulateScheduleDataAsync(application);

        return View(new InterviewScheduleViewModel
        {
            ApplicationId = applicationId,
            InterviewStageId = application.CurrentInterviewStageId
                ?? application.Vacancy.InterviewStages.OrderBy(s => s.StageOrder).First().InterviewStageId,
            ScheduledAt = DateTime.Now.AddDays(1).Date.AddHours(10),
            DurationMinutes = 60
        });
    }
    [Authorize(Roles = "HR")]
    [HttpGet]
    public async Task<IActionResult> AvailableInterviewers(
    DateTime scheduledAt,
    int durationMinutes)
    {
        if (durationMinutes < 15 || durationMinutes > 480)
        {
            return BadRequest();
        }

        var requestedStart = scheduledAt;
        var requestedEnd = scheduledAt.AddMinutes(durationMinutes);

        var interviewers = (await _userManager.GetUsersInRoleAsync("Interviewer"))
            .OrderBy(u => u.FullName)
            .ToList();

        var interviewerIds = interviewers
            .Select(u => u.Id)
            .ToList();

        var possibleConflicts = await _db.Interviews
            .Where(i =>
                interviewerIds.Contains(i.InterviewerId) &&
                i.Status != "Cancelled" &&
                i.ScheduledAt < requestedEnd)
            .Select(i => new
            {
                i.InterviewerId,
                i.ScheduledAt,
                i.DurationMinutes
            })
            .ToListAsync();

        var result = interviewers.Select(person =>
        {
            var hasConflict = possibleConflicts.Any(i =>
                i.InterviewerId == person.Id &&
                i.ScheduledAt.AddMinutes(i.DurationMinutes) > requestedStart);

            return new
            {
                id = person.Id,
                name = person.FullName,
                email = person.Email,
                isAvailable = !hasConflict
            };
        });

        return Json(result);
    }
    [Authorize(Roles = "HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Schedule(InterviewScheduleViewModel model)
    {
        var application = await _db.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Vacancy)!.ThenInclude(v => v!.InterviewStages)
            .Include(a => a.Interviews)!.ThenInclude(i => i.Feedback)
            .FirstOrDefaultAsync(a => a.ApplicationId == model.ApplicationId);

        if (application?.Vacancy == null || application.Candidate == null)
            return NotFound();

        var stage = application.Vacancy.InterviewStages
            .FirstOrDefault(s => s.InterviewStageId == model.InterviewStageId);

        if (stage == null)
            ModelState.AddModelError(nameof(model.InterviewStageId), "Select a valid stage for this vacancy.");

        var interviewer = await _userManager.FindByIdAsync(model.InterviewerId);
        if (interviewer == null || !await _userManager.IsInRoleAsync(interviewer, "Interviewer"))
            ModelState.AddModelError(nameof(model.InterviewerId), "Select a valid interviewer.");
        if (interviewer != null &&
    await _userManager.IsInRoleAsync(interviewer, "Interviewer") &&
    model.DurationMinutes >= 15 &&
    model.DurationMinutes <= 480)
        {
            var requestedStart = model.ScheduledAt;
            var requestedEnd = model.ScheduledAt.AddMinutes(model.DurationMinutes);

            var possibleConflicts = await _db.Interviews
                .Where(i =>
                    i.InterviewerId == model.InterviewerId &&
                    i.Status != "Cancelled" &&
                    i.ScheduledAt < requestedEnd)
                .Select(i => new
                {
                    i.ScheduledAt,
                    i.DurationMinutes
                })
                .ToListAsync();

            var hasConflict = possibleConflicts.Any(i =>
                i.ScheduledAt.AddMinutes(i.DurationMinutes) > requestedStart);

            if (hasConflict)
            {
                ModelState.AddModelError(
                    nameof(model.InterviewerId),
                    "This interviewer is unavailable during the selected time. Please choose another interviewer.");
            }
        }
        if (model.ScheduledAt <= DateTime.Now)
            ModelState.AddModelError(nameof(model.ScheduledAt), "The interview must be scheduled in the future.");

        if (stage != null)
        {
            var duplicate = application.Interviews.Any(i =>
                i.InterviewStageId == stage.InterviewStageId &&
                i.Status != "Cancelled");

            if (duplicate)
                ModelState.AddModelError(nameof(model.InterviewStageId),
                    "This candidate already has an active interview for that stage.");

            if (application.Vacancy.RequireFeedbackBeforeAdvance)
            {
                var previousStage = application.Vacancy.InterviewStages
                    .Where(s => s.StageOrder < stage.StageOrder)
                    .OrderByDescending(s => s.StageOrder)
                    .FirstOrDefault();

                if (previousStage != null)
                {
                    var previousCompleted = application.Interviews.Any(i =>
                        i.InterviewStageId == previousStage.InterviewStageId &&
                        i.Status == "Completed" &&
                        i.Feedback != null);

                    if (!previousCompleted)
                    {
                        ModelState.AddModelError(nameof(model.InterviewStageId),
                            $"Feedback for '{previousStage.StageName}' is required before advancing to '{stage.StageName}'.");
                    }
                }
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateScheduleDataAsync(application);
            return View(model);
        }

        var interview = new Interview
        {
            ApplicationId = application.ApplicationId,
            InterviewStageId = model.InterviewStageId,
            InterviewerId = model.InterviewerId,
            ScheduledAt = model.ScheduledAt,
            DurationMinutes = model.DurationMinutes,
            LocationOrMeetingLink = model.LocationOrMeetingLink,
            Notes = model.Notes,
            Status = "Scheduled",
            CreatedDate = DateTime.UtcNow
        };

        _db.Interviews.Add(interview);
        application.Status = "Interview";
        application.CurrentInterviewStageId = model.InterviewStageId;
        application.Candidate.Status = "Interview";
        await _db.SaveChangesAsync();

        var localWhen = model.ScheduledAt.ToString("dd MMM yyyy HH:mm");
        await _notifications.NotifyUserAsync(
            model.InterviewerId,
            "Interview assigned",
            $"You have been assigned to interview {application.Candidate.FullName} for {application.Vacancy.JobTitle} on {localWhen}.",
            $"/Interview/Details/{interview.InterviewId}");

        await _notifications.NotifyUserAsync(
            application.Candidate.ApplicationUserId,
            "Interview scheduled",
            $"Your {stage!.StageName} interview for {application.Vacancy.JobTitle} is scheduled for {localWhen}.",
            $"/Interview/Details/{interview.InterviewId}");

        TempData["Success"] = "Interview scheduled and both the candidate and interviewer were notified in the system.";
        return RedirectToAction("Details", "Application", new { id = application.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var interview = await _db.Interviews
            .Include(i => i.Application)!.ThenInclude(a => a!.Candidate)
            .Include(i => i.Application)!.ThenInclude(a => a!.Vacancy)
            .Include(i => i.InterviewStage)
            .Include(i => i.Interviewer)
            .Include(i => i.Feedback)
            .FirstOrDefaultAsync(i => i.InterviewId == id);

        if (interview?.Application?.Candidate == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);

        if (User.IsInRole("Interviewer") && interview.InterviewerId != user?.Id)
            return Forbid();

        if (User.IsInRole("Candidate") &&
            interview.Application.Candidate.ApplicationUserId != user?.Id)
            return Forbid();

        return View(interview);
    }

    [Authorize(Roles = "Interviewer")]
    [HttpGet]
    public async Task<IActionResult> Feedback(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var interview = await _db.Interviews
            .Include(i => i.Application)!.ThenInclude(a => a!.Candidate)
            .Include(i => i.Application)!.ThenInclude(a => a!.Vacancy)
            .Include(i => i.InterviewStage)
            .Include(i => i.Feedback)
            .FirstOrDefaultAsync(i => i.InterviewId == id);

        if (interview == null) return NotFound();
        if (interview.InterviewerId != user?.Id) return Forbid();
        if (interview.Status == "Cancelled")
        {
            TempData["Error"] = "Feedback cannot be submitted for a cancelled interview.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var existing = interview.Feedback;

        return View(new InterviewFeedbackViewModel
        {
            InterviewId = id,
            TechnicalScore = existing?.TechnicalScore ?? 3,
            CommunicationScore = existing?.CommunicationScore ?? 3,
            ProblemSolvingScore = existing?.ProblemSolvingScore ?? 3,
            CultureFitScore = existing?.CultureFitScore ?? 3,
            Recommendation = existing?.Recommendation ?? "Hold",
            Comments = existing?.Comments ?? string.Empty
        });
    }

    [Authorize(Roles = "Interviewer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Feedback(InterviewFeedbackViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        var interview = await _db.Interviews
            .Include(i => i.Application)!.ThenInclude(a => a!.Candidate)
            .Include(i => i.Application)!.ThenInclude(a => a!.Vacancy)
            .Include(i => i.InterviewStage)
            .Include(i => i.Feedback)
            .FirstOrDefaultAsync(i => i.InterviewId == model.InterviewId);

        if (interview?.Application?.Candidate == null || interview.Application.Vacancy == null)
            return NotFound();

        if (interview.InterviewerId != user?.Id) return Forbid();

        if (!ModelState.IsValid)
            return View(model);

        if (interview.Feedback == null)
        {
            interview.Feedback = new InterviewFeedback
            {
                InterviewId = interview.InterviewId,
                InterviewerId = user!.Id
            };
        }

        interview.Feedback.TechnicalScore = model.TechnicalScore;
        interview.Feedback.CommunicationScore = model.CommunicationScore;
        interview.Feedback.ProblemSolvingScore = model.ProblemSolvingScore;
        interview.Feedback.CultureFitScore = model.CultureFitScore;
        interview.Feedback.Recommendation = model.Recommendation;
        interview.Feedback.Comments = model.Comments;
        interview.Feedback.SubmittedAt = DateTime.UtcNow;
        interview.Status = "Completed";

        await _db.SaveChangesAsync();

        var message =
            $"{user!.FullName} submitted feedback for {interview.Application.Candidate.FullName} " +
            $"({interview.Application.Vacancy.JobTitle} - {interview.InterviewStage?.StageName}).";

        await _notifications.NotifyRoleAsync(
            "HR",
            "Interview feedback submitted",
            message,
            $"/Application/Details/{interview.ApplicationId}");

        await _notifications.NotifyRoleAsync(
            "HiringManager",
            "Interview feedback submitted",
            message,
            $"/Application/Details/{interview.ApplicationId}");

        TempData["Success"] = "Feedback submitted successfully.";
        return RedirectToAction(nameof(Details), new { id = interview.InterviewId });
    }

    [Authorize(Roles = "HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var interview = await _db.Interviews
            .Include(i => i.Application)!.ThenInclude(a => a!.Candidate)
            .Include(i => i.Application)!.ThenInclude(a => a!.Vacancy)
            .Include(i => i.InterviewStage)
            .FirstOrDefaultAsync(i => i.InterviewId == id);

        if (interview?.Application?.Candidate == null || interview.Application.Vacancy == null)
            return NotFound();

        if (interview.Status == "Completed")
        {
            TempData["Error"] = "A completed interview cannot be cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        interview.Status = "Cancelled";
        await _db.SaveChangesAsync();

        await _notifications.NotifyUserAsync(
            interview.InterviewerId,
            "Interview cancelled",
            $"The interview with {interview.Application.Candidate.FullName} for {interview.Application.Vacancy.JobTitle} has been cancelled.");

        await _notifications.NotifyUserAsync(
            interview.Application.Candidate.ApplicationUserId,
            "Interview cancelled",
            $"Your interview for {interview.Application.Vacancy.JobTitle} has been cancelled.");

        TempData["Success"] = "Interview cancelled.";
        return RedirectToAction("Details", "Application", new { id = interview.ApplicationId });
    }

    private async Task PopulateScheduleDataAsync(Application application)
    {
        ViewBag.Application = application;
        ViewBag.Stages = application.Vacancy?.InterviewStages
            .OrderBy(s => s.StageOrder)
            .ToList() ?? new List<InterviewStage>();

        ViewBag.Interviewers = (await _userManager.GetUsersInRoleAsync("Interviewer"))
            .OrderBy(u => u.FullName)
            .ToList();
    }
}
