using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers;

[Authorize(Roles = "HR")]
public class InterviewStageController : Controller
{
    private readonly ApplicationDbContext _db;

    public InterviewStageController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(int vacancyId)
    {
        var vacancy = await _db.Vacancies
            .Include(v => v.InterviewStages.OrderBy(s => s.StageOrder))
            .FirstOrDefaultAsync(v => v.VacancyId == vacancyId);

        return vacancy == null ? NotFound() : View(vacancy);
    }

    public IActionResult Create(int vacancyId)
        => View(new InterviewStage { VacancyId = vacancyId });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InterviewStage model)
    {
        ModelState.Remove(nameof(InterviewStage.Vacancy));

        var duplicateOrder = await _db.InterviewStages.AnyAsync(s =>
            s.VacancyId == model.VacancyId && s.StageOrder == model.StageOrder);

        if (duplicateOrder)
            ModelState.AddModelError(nameof(model.StageOrder),
                "Another stage already uses this order number.");

        if (!ModelState.IsValid) return View(model);

        _db.InterviewStages.Add(model);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Interview stage added.";
        return RedirectToAction(nameof(Index), new { vacancyId = model.VacancyId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var stage = await _db.InterviewStages.FindAsync(id);
        return stage == null ? NotFound() : View(stage);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InterviewStage model)
    {
        if (id != model.InterviewStageId) return BadRequest();

        ModelState.Remove(nameof(InterviewStage.Vacancy));

        var duplicateOrder = await _db.InterviewStages.AnyAsync(s =>
            s.VacancyId == model.VacancyId &&
            s.StageOrder == model.StageOrder &&
            s.InterviewStageId != model.InterviewStageId);

        if (duplicateOrder)
            ModelState.AddModelError(nameof(model.StageOrder),
                "Another stage already uses this order number.");

        if (!ModelState.IsValid) return View(model);

        var stage = await _db.InterviewStages.FindAsync(id);
        if (stage == null) return NotFound();

        stage.StageName = model.StageName;
        stage.StageOrder = model.StageOrder;
        stage.Description = model.Description;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Interview stage updated.";
        return RedirectToAction(nameof(Index), new { vacancyId = stage.VacancyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var stage = await _db.InterviewStages.FindAsync(id);
        if (stage == null) return NotFound();

        var inUse = await _db.Interviews.AnyAsync(i => i.InterviewStageId == id)
                    || await _db.Applications.AnyAsync(a => a.CurrentInterviewStageId == id);

        if (inUse)
        {
            TempData["Error"] = "This stage is already used by a candidate and cannot be deleted.";
            return RedirectToAction(nameof(Index), new { vacancyId = stage.VacancyId });
        }

        var vacancyId = stage.VacancyId;
        _db.InterviewStages.Remove(stage);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Interview stage deleted.";
        return RedirectToAction(nameof(Index), new { vacancyId });
    }
}
