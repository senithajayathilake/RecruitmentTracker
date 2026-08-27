using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;
using RecruitmentTracker.Services;

namespace RecruitmentTracker.Controllers;

[Authorize(Roles = "HR")]
public class CandidateFilterController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICandidateFilterService _filterService;

    public CandidateFilterController(ApplicationDbContext db, ICandidateFilterService filterService)
    {
        _db = db;
        _filterService = filterService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.Vacancies = await _db.Vacancies
            .Where(v => v.Status == "Open")
            .OrderBy(v => v.JobTitle)
            .ToListAsync();

        return View(new CandidateFilterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CandidateFilterViewModel model)
    {
        ViewBag.Vacancies = await _db.Vacancies
            .Where(v => v.Status == "Open")
            .OrderBy(v => v.JobTitle)
            .ToListAsync();

        var vacancy = await _db.Vacancies.FindAsync(model.VacancyId);
        if (vacancy == null)
        {
            ModelState.AddModelError(nameof(model.VacancyId), "Vacancy not found.");
            return View(model);
        }

        var candidates = await _db.Candidates
            .Include(c => c.Cvs)
            .ToListAsync();

        model.Results = await _filterService.FilterAsync(vacancy, candidates);
        await _db.SaveChangesAsync();

        ViewBag.SelectedVacancy = vacancy.JobTitle;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Shortlist(int candidateId)
    {
        var candidate = await _db.Candidates.FindAsync(candidateId);
        if (candidate == null) return NotFound();

        candidate.Status = "Shortlisted";
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int candidateId)
    {
        var candidate = await _db.Candidates.FindAsync(candidateId);
        if (candidate == null) return NotFound();

        candidate.Status = "Rejected";
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
