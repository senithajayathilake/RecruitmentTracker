using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers;

[Authorize(Roles = "HR,HiringManager")]
public class ComparisonController : Controller
{
    private readonly ApplicationDbContext _db;

    public ComparisonController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(
    int? vacancyId = null,
    List<int>? selectedApplicationIds = null)
    {
        var vacancies = await _db.Vacancies
            .OrderByDescending(v => v.CreatedDate)
            .ToListAsync();

        var vm = new CandidateComparisonViewModel
        {
            Vacancies = vacancies,
            SelectedApplicationIds = selectedApplicationIds ?? new List<int>()
        };

        // If no vacancy was selected, use the newest vacancy
        if (!vacancyId.HasValue && vacancies.Count > 0)
        {
            vacancyId = vacancies.First().VacancyId;
        }

        if (!vacancyId.HasValue)
        {
            return View(vm);
        }

        vm.SelectedVacancy = vacancies
            .FirstOrDefault(v => v.VacancyId == vacancyId.Value);

        if (vm.SelectedVacancy == null)
        {
            return View(vm);
        }

        // Load all applications for the selected vacancy
        var applications = await _db.Applications
            .Where(a => a.VacancyId == vacancyId.Value)
            .Include(a => a.Candidate)
            .Include(a => a.CurrentInterviewStage)
            .Include(a => a.Interviews)!
                .ThenInclude(i => i.Feedback)
            .OrderByDescending(a => a.AiScore)
            .ToListAsync();

        // Convert each application into a comparison row
        var allCandidates = applications.Select(a =>
        {
            var feedback = a.Interviews
                .Where(i => i.Feedback != null)
                .OrderBy(i => i.ScheduledAt)
                .Select(i => i.Feedback!)
                .ToList();

            return new CandidateComparisonRowViewModel
            {
                ApplicationId = a.ApplicationId,

                CandidateName =
                    a.Candidate?.FullName ?? "",

                Email =
                    a.Candidate?.Email ?? "",

                YearsOfExperience =
                    a.Candidate?.YearsOfExperience ?? 0,

                Skills =
                    a.Candidate?.Skills ?? "",

                AiScore =
                    a.AiScore ?? 0,

                InterviewAverage =
                    feedback.Count == 0
                        ? null
                        : Math.Round(
                            feedback.Average(f => f.AverageScore),
                            1),

                LatestRecommendation =
                    feedback.LastOrDefault()?.Recommendation
                    ?? "No feedback",

                CurrentStage =
                    a.CurrentInterviewStage?.StageName
                    ?? "Not started",

                Status =
                    a.Status
            };

        }).ToList();

        // Everyone under the vacancy is available for selection
        vm.AvailableCandidates = allCandidates;

        // Only selected candidates are displayed in the final comparison
        if (vm.SelectedApplicationIds.Any())
        {
            vm.Rows = allCandidates
                .Where(c =>
                    vm.SelectedApplicationIds.Contains(
                        c.ApplicationId))
                .ToList();
        }
        else
        {
            vm.Rows = new List<CandidateComparisonRowViewModel>();
        }

        return View(vm);
    }
    }