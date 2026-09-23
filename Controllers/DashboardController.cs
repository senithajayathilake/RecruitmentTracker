using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize(Roles = "HR")]
    public async Task<IActionResult> HR()
    {
        var now = DateTime.Now;
        var staleCutoff = DateTime.UtcNow.AddDays(-7);

        // 1. Shortlisted candidates with no active interview scheduled
        var awaitingInterviewApplications = await _db.Applications
            .Where(a =>
                a.Status == "Shortlisted" &&
                !a.Interviews.Any(i => i.Status != "Cancelled"))
            .Include(a => a.Candidate)
            .Include(a => a.Vacancy)
            .ToListAsync();

        // 2. Interviews that should already be finished but have no feedback
        var possibleOverdueFeedback = await _db.Interviews
            .Where(i =>
                i.Status == "Scheduled" &&
                i.Feedback == null &&
                i.ScheduledAt <= now)
            .Include(i => i.Application)!.ThenInclude(a => a!.Candidate)
            .Include(i => i.Application)!.ThenInclude(a => a!.Vacancy)
            .ToListAsync();

        var overdueFeedbackInterviews = possibleOverdueFeedback
            .Where(i =>
                i.ScheduledAt.AddMinutes(i.DurationMinutes) <= now)
            .ToList();

        // 3. Applications open for more than 7 days without a final decision
        var longOpenApplications = await _db.Applications
            .Where(a =>
                a.AppliedDate <= staleCutoff &&
                a.Status != "Hired" &&
                a.Status != "Rejected" &&
                a.Status != "On Hold")
            .Include(a => a.Candidate)
            .Include(a => a.Vacancy)
            .ToListAsync();

        // Build the detailed bottleneck list
        var bottlenecks = new List<RecruitmentBottleneckViewModel>();

        foreach (var application in awaitingInterviewApplications)
        {
            bottlenecks.Add(new RecruitmentBottleneckViewModel
            {
                Type = "Awaiting Interview",
                CandidateName = application.Candidate?.FullName ?? "Unknown candidate",
                VacancyTitle = application.Vacancy?.JobTitle ?? "Unknown vacancy",
                Description = "Candidate is shortlisted but no interview has been scheduled.",
                ApplicationId = application.ApplicationId
            });
        }

        foreach (var interview in overdueFeedbackInterviews)
        {
            bottlenecks.Add(new RecruitmentBottleneckViewModel
            {
                Type = "Awaiting Feedback",
                CandidateName = interview.Application?.Candidate?.FullName ?? "Unknown candidate",
                VacancyTitle = interview.Application?.Vacancy?.JobTitle ?? "Unknown vacancy",
                Description =
                    $"Interview was scheduled for {interview.ScheduledAt:dd MMM yyyy HH:mm} but feedback has not been submitted.",
                ApplicationId = interview.ApplicationId
            });
        }

        foreach (var application in longOpenApplications)
        {
            bottlenecks.Add(new RecruitmentBottleneckViewModel
            {
                Type = "Long-Open Application",
                CandidateName = application.Candidate?.FullName ?? "Unknown candidate",
                VacancyTitle = application.Vacancy?.JobTitle ?? "Unknown vacancy",
                Description =
                    $"Application has remained active since {application.AppliedDate:dd MMM yyyy}.",
                ApplicationId = application.ApplicationId
            });
        }

        var vm = new DashboardViewModel
        {
            VacancyCount = await _db.Vacancies.CountAsync(),

            CandidateCount = await _db.Candidates.CountAsync(),

            ApplicationCount = await _db.Applications.CountAsync(),

            ShortlistedCount = await _db.Applications
                .CountAsync(a => a.Status == "Shortlisted"),

            PendingCount = await _db.Applications
                .CountAsync(a =>
                    a.Status == "New" ||
                    a.Status == "Under Review"),

            RejectedCount = await _db.Applications
                .CountAsync(a => a.Status == "Rejected"),

            UpcomingInterviewCount = await _db.Interviews
                .CountAsync(i =>
                    i.Status == "Scheduled" &&
                    i.ScheduledAt >= now),

            PendingFeedbackCount =
                overdueFeedbackInterviews.Count,

            // Sprint 2 bottleneck metrics
            AwaitingInterviewCount =
                awaitingInterviewApplications.Count,

            OverdueFeedbackCount =
                overdueFeedbackInterviews.Count,

            LongOpenApplicationCount =
                longOpenApplications.Count,

            Bottlenecks = bottlenecks,

            RecentApplications = await _db.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Vacancy)
                .Include(a => a.CandidateCv)
                .OrderByDescending(a => a.AppliedDate)
                .Take(8)
                .ToListAsync(),

            UpcomingInterviews = await _db.Interviews
                .Where(i =>
                    i.Status == "Scheduled" &&
                    i.ScheduledAt >= now)
                .Include(i => i.Application)!
                    .ThenInclude(a => a!.Candidate)
                .Include(i => i.Application)!
                    .ThenInclude(a => a!.Vacancy)
                .Include(i => i.InterviewStage)
                .Include(i => i.Interviewer)
                .OrderBy(i => i.ScheduledAt)
                .Take(6)
                .ToListAsync()
        };

        return View(vm);
    }

    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Candidate()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var candidate = await _db.Candidates
            .Include(c => c.Cvs.OrderByDescending(cv => cv.UploadedDate))
            .Include(c => c.Applications.OrderByDescending(a => a.AppliedDate))
                .ThenInclude(a => a.Vacancy)
            .FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);

        if (candidate == null)
        {
            candidate = new Candidate
            {
                ApplicationUserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty
            };
            _db.Candidates.Add(candidate);
            await _db.SaveChangesAsync();
        }

        var appliedIds = candidate.Applications.Select(a => a.VacancyId).ToHashSet();

        var applicationIds = candidate.Applications.Select(a => a.ApplicationId).ToList();

        var vm = new CandidateDashboardViewModel
        {
            Candidate = candidate,
            Cvs = candidate.Cvs.ToList(),
            Applications = candidate.Applications.ToList(),
            OpenVacancies = await _db.Vacancies
                .Where(v => v.Status == "Open" && v.ApplicationDeadline >= DateTime.Today)
                .OrderBy(v => v.ApplicationDeadline)
                .ToListAsync(),
            UpcomingInterviews = await _db.Interviews
                .Where(i => applicationIds.Contains(i.ApplicationId) &&
                            i.Status == "Scheduled" &&
                            i.ScheduledAt >= DateTime.Now)
                .Include(i => i.Application)!.ThenInclude(a => a!.Vacancy)
                .Include(i => i.InterviewStage)
                .Include(i => i.Interviewer)
                .OrderBy(i => i.ScheduledAt)
                .ToListAsync(),
            Notifications = await _db.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(8)
                .ToListAsync()
        };

        ViewBag.AppliedVacancyIds = appliedIds;
        return View(vm);
    }

    [Authorize(Roles = "Interviewer")]
    public async Task<IActionResult> Interviewer()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var interviews = await _db.Interviews
            .Where(i => i.InterviewerId == user.Id)
            .Include(i => i.Application)!.ThenInclude(a => a!.Candidate)
            .Include(i => i.Application)!.ThenInclude(a => a!.Vacancy)
            .Include(i => i.InterviewStage)
            .Include(i => i.Feedback)
            .OrderBy(i => i.Status == "Scheduled" ? 0 : 1)
            .ThenBy(i => i.ScheduledAt)
            .ToListAsync();

        var vm = new InterviewerDashboardViewModel
        {
            Interviews = interviews,
            PendingFeedbackCount = interviews.Count(i => i.Status == "Scheduled" && i.Feedback == null),
            Notifications = await _db.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(8)
                .ToListAsync()
        };

        return View(vm);
    }

    [Authorize(Roles = "HiringManager")]
    public async Task<IActionResult> Manager()
    {
        var vm = new HiringManagerDashboardViewModel
        {
            Applications = await _db.Applications
                .Include(a => a.Candidate)
                    .ThenInclude(c => c!.Cvs)
                .Include(a => a.Vacancy)
                .Include(a => a.CandidateCv)
                .Include(a => a.CurrentInterviewStage)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync(),
            OpenVacancies = await _db.Vacancies.CountAsync(v => v.Status == "Open"),
            HiredCount = await _db.Applications.CountAsync(a => a.Status == "Hired"),
            PendingDecisions = await _db.Applications.CountAsync(a =>
                a.Status != "Hired" && a.Status != "Rejected"),
            CompletedInterviews = await _db.Interviews.CountAsync(i => i.Status == "Completed")
        };

        return View(vm);
    }
}
