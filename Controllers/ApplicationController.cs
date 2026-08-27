using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;
using RecruitmentTracker.Services;

namespace RecruitmentTracker.Controllers;

[Authorize]
public class ApplicationController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAiCvScreeningService _ai;

    public ApplicationController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IAiCvScreeningService ai)
    {
        _db = db;
        _userManager = userManager;
        _ai = ai;
    }

    [Authorize(Roles = "Candidate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(int vacancyId)
    {
        var user = await _userManager.GetUserAsync(User);
        var candidate = await _db.Candidates
            .Include(c => c.Cvs)
            .FirstOrDefaultAsync(c => c.ApplicationUserId == user!.Id);

        var vacancy = await _db.Vacancies.FindAsync(vacancyId);

        if (candidate == null || vacancy == null) return NotFound();

        if (vacancy.Status != "Open" || vacancy.ApplicationDeadline.Date < DateTime.Today)
        {
            TempData["Error"] = "This vacancy is no longer accepting applications.";
            return RedirectToAction("Browse", "Vacancy");
        }

        if (await _db.Applications.AnyAsync(a => a.CandidateId == candidate.CandidateId && a.VacancyId == vacancyId))
        {
            TempData["Error"] = "You have already applied for this vacancy.";
            return RedirectToAction("Candidate", "Dashboard");
        }

        var cv = candidate.Cvs.OrderByDescending(c => c.UploadedDate).FirstOrDefault();
        if (cv == null)
        {
            TempData["Error"] = "Please upload your CV before applying.";
            return RedirectToAction("Upload", "CV");
        }

        var ai = await _ai.ScreenAsync(candidate, vacancy, cv);

        var application = new Application
        {
            CandidateId = candidate.CandidateId,
            VacancyId = vacancy.VacancyId,
            CandidateCvId = cv.CandidateCvId,
            Status = "New",
            AiScore = ai.Score,
            AiSummary = ai.Summary,
            MatchedSkills = ai.MatchedSkills,
            MissingSkills = ai.MissingSkills
        };

        candidate.Status = "Screening";
        _db.Applications.Add(application);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Application submitted. HR can now see your application and AI screening result.";
        return RedirectToAction("Candidate", "Dashboard");
    }

    [Authorize(Roles = "HR,HiringManager")]
    public async Task<IActionResult> Details(int id)
    {
        var application = await _db.Applications
            .Include(a => a.Candidate)
                .ThenInclude(c => c!.Cvs)
            .Include(a => a.CandidateCv)
            .Include(a => a.Vacancy)
            .FirstOrDefaultAsync(a => a.ApplicationId == id);

        return application == null ? NotFound() : View(application);
    }

    [Authorize(Roles = "HR,HiringManager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, string status)
    {
        var application = await _db.Applications
            .Include(a => a.Candidate)
            .FirstOrDefaultAsync(a => a.ApplicationId == id);

        if (application == null) return NotFound();

        application.Status = status;
        if (application.Candidate != null)
            application.Candidate.Status = status;

        await _db.SaveChangesAsync();

        return RedirectToAction("Details", new { id });
    }

    [Authorize(Roles = "HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunAiScreening(int id)
    {
        var application = await _db.Applications
            .Include(a => a.Candidate)
            .Include(a => a.CandidateCv)
            .Include(a => a.Vacancy)
            .FirstOrDefaultAsync(a => a.ApplicationId == id);

        if (application?.Candidate == null || application.CandidateCv == null || application.Vacancy == null)
            return NotFound();

        var ai = await _ai.ScreenAsync(application.Candidate, application.Vacancy, application.CandidateCv);
        application.AiScore = ai.Score;
        application.AiSummary = ai.Summary;
        application.MatchedSkills = ai.MatchedSkills;
        application.MissingSkills = ai.MissingSkills;

        application.CandidateCv.AiScore = ai.Score;
        application.CandidateCv.AiSummary = ai.Summary;
        application.CandidateCv.MatchedSkills = ai.MatchedSkills;
        application.CandidateCv.MissingSkills = ai.MissingSkills;

        await _db.SaveChangesAsync();

        TempData["Success"] = "AI screening completed.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
