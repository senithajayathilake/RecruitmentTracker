using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers;

[Authorize]
public class CandidateController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CandidateController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Index()
        => View(await _db.Candidates
            .Include(c => c.Cvs)
            .Include(c => c.Applications)
                .ThenInclude(a => a.Vacancy)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync());

    [Authorize(Roles = "HR")]
    public IActionResult Create() => View(new Candidate());

    [Authorize(Roles = "HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Candidate model)
    {
        if (!ModelState.IsValid) return View(model);

        _db.Candidates.Add(model);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Candidate added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Details(int id)
    {
        var candidate = await _db.Candidates
            .Include(c => c.Cvs.OrderByDescending(x => x.UploadedDate))
            .Include(c => c.Applications)
                .ThenInclude(a => a.Vacancy)
            .FirstOrDefaultAsync(c => c.CandidateId == id);

        return candidate == null ? NotFound() : View(candidate);
    }

    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.ApplicationUserId == user!.Id);
        return candidate == null ? NotFound() : View(candidate);
    }

    [Authorize(Roles = "Candidate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(Candidate model)
    {
        var user = await _userManager.GetUserAsync(User);
        var candidate = await _db.Candidates.FirstOrDefaultAsync(c => c.ApplicationUserId == user!.Id);
        if (candidate == null) return NotFound();

        candidate.FullName = model.FullName;
        candidate.Email = user!.Email ?? candidate.Email;
        candidate.Phone = model.Phone;
        candidate.Address = model.Address;
        candidate.DateOfBirth = model.DateOfBirth;
        candidate.HighestQualification = model.HighestQualification;
        candidate.YearsOfExperience = model.YearsOfExperience;
        candidate.Skills = model.Skills;

        user.FullName = candidate.FullName;
        await _userManager.UpdateAsync(user);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize(Roles = "HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, string status)
    {
        var candidate = await _db.Candidates.FindAsync(id);
        if (candidate == null) return NotFound();

        candidate.Status = status;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }
}
