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
        var vm = new DashboardViewModel
        {
            VacancyCount = await _db.Vacancies.CountAsync(),
            CandidateCount = await _db.Candidates.CountAsync(),
            ApplicationCount = await _db.Applications.CountAsync(),
            ShortlistedCount = await _db.Applications.CountAsync(a => a.Status == "Shortlisted"),
            PendingCount = await _db.Applications.CountAsync(a => a.Status == "New" || a.Status == "Under Review"),
            RejectedCount = await _db.Applications.CountAsync(a => a.Status == "Rejected"),
            RecentApplications = await _db.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Vacancy)
                .Include(a => a.CandidateCv)
                .OrderByDescending(a => a.AppliedDate)
                .Take(8)
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
        var vm = new CandidateDashboardViewModel
        {
            Candidate = candidate,
            Cvs = candidate.Cvs.ToList(),
            Applications = candidate.Applications.ToList(),
            OpenVacancies = await _db.Vacancies
                .Where(v => v.Status == "Open" && v.ApplicationDeadline >= DateTime.Today)
                .OrderBy(v => v.ApplicationDeadline)
                .ToListAsync()
        };

        ViewBag.AppliedVacancyIds = appliedIds;
        return View(vm);
    }

    [Authorize(Roles = "Interviewer")]
    public IActionResult Interviewer() => View();

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
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync()
        };
        return View(vm);
    }
}
