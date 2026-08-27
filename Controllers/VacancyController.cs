using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers;

[Authorize]
public class VacancyController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public VacancyController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Index()
        => View(await _db.Vacancies
            .Include(v => v.InterviewStages)
            .Include(v => v.Applications)
            .OrderByDescending(v => v.CreatedDate)
            .ToListAsync());

    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Browse()
        => View(await _db.Vacancies
            .Where(v => v.Status == "Open" && v.ApplicationDeadline >= DateTime.Today)
            .OrderBy(v => v.ApplicationDeadline)
            .ToListAsync());

    [Authorize(Roles = "HR,Candidate,HiringManager")]
    public async Task<IActionResult> Details(int id)
    {
        var vacancy = await _db.Vacancies
            .Include(v => v.InterviewStages.OrderBy(s => s.StageOrder))
            .Include(v => v.Applications)
            .FirstOrDefaultAsync(v => v.VacancyId == id);

        return vacancy == null ? NotFound() : View(vacancy);
    }

    [Authorize(Roles = "HR")]
    public IActionResult Create() => View(new Vacancy());

    [Authorize(Roles = "HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Vacancy model)
    {
        ModelState.Remove(nameof(Vacancy.CreatedByUserId));
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        model.CreatedByUserId = user!.Id;
        model.CreatedDate = DateTime.UtcNow;
        model.Status = "Open";

        _db.Vacancies.Add(model);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Vacancy created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Edit(int id)
    {
        var vacancy = await _db.Vacancies.FindAsync(id);
        return vacancy == null ? NotFound() : View(vacancy);
    }

    [Authorize(Roles = "HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Vacancy model)
    {
        if (id != model.VacancyId) return BadRequest();

        ModelState.Remove(nameof(Vacancy.CreatedByUserId));
        if (!ModelState.IsValid) return View(model);

        var vacancy = await _db.Vacancies.FindAsync(id);
        if (vacancy == null) return NotFound();

        vacancy.JobTitle = model.JobTitle;
        vacancy.Department = model.Department;
        vacancy.Location = model.Location;
        vacancy.EmploymentType = model.EmploymentType;
        vacancy.Salary = model.Salary;
        vacancy.Description = model.Description;
        vacancy.Requirements = model.Requirements;
        vacancy.NumberOfPositions = model.NumberOfPositions;
        vacancy.ApplicationDeadline = model.ApplicationDeadline;
        vacancy.Status = model.Status;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Vacancy updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var vacancy = await _db.Vacancies.FindAsync(id);
        if (vacancy == null) return NotFound();

        vacancy.Status = "Closed";
        await _db.SaveChangesAsync();

        TempData["Success"] = "Vacancy closed.";
        return RedirectToAction(nameof(Index));
    }
}
