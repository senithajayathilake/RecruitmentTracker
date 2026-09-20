using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers;

[Authorize(Roles = "HR,HiringManager")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ReportsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await BuildReportAsync());
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv()
    {
        var report = await BuildReportAsync();

        static string Csv(string? value)
            => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

        var sb = new StringBuilder();
        sb.AppendLine("Recruitment Summary");
        sb.AppendLine($"Open Vacancies,{report.OpenVacancies}");
        sb.AppendLine($"Total Applications,{report.TotalApplications}");
        sb.AppendLine($"Hired,{report.Hired}");
        sb.AppendLine($"Rejected,{report.Rejected}");
        sb.AppendLine($"On Hold,{report.OnHold}");
        sb.AppendLine($"Scheduled Interviews,{report.ScheduledInterviews}");
        sb.AppendLine($"Completed Interviews,{report.CompletedInterviews}");
        sb.AppendLine();
        sb.AppendLine("Vacancy,Department,Status,Applications,Hired,Rejected,Average AI Score");

        foreach (var row in report.VacancyRows)
        {
            sb.AppendLine(
                $"{Csv(row.JobTitle)},{Csv(row.Department)},{Csv(row.Status)}," +
                $"{row.Applications},{row.Hired},{row.Rejected},{row.AverageAiScore:0.0}");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var fileName = $"RecruitmentReport_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv", fileName);
    }

    private async Task<ReportsViewModel> BuildReportAsync()
    {
        var vacancies = await _db.Vacancies
            .Include(v => v.Applications)
            .OrderByDescending(v => v.CreatedDate)
            .ToListAsync();

        return new ReportsViewModel
        {
            OpenVacancies = vacancies.Count(v => v.Status == "Open"),
            TotalApplications = await _db.Applications.CountAsync(),
            Hired = await _db.Applications.CountAsync(a => a.Status == "Hired"),
            Rejected = await _db.Applications.CountAsync(a => a.Status == "Rejected"),
            OnHold = await _db.Applications.CountAsync(a => a.Status == "On Hold"),
            ScheduledInterviews = await _db.Interviews.CountAsync(i => i.Status == "Scheduled"),
            CompletedInterviews = await _db.Interviews.CountAsync(i => i.Status == "Completed"),
            VacancyRows = vacancies.Select(v => new VacancyReportRowViewModel
            {
                VacancyId = v.VacancyId,
                JobTitle = v.JobTitle,
                Department = v.Department,
                Status = v.Status,
                Applications = v.Applications.Count,
                Hired = v.Applications.Count(a => a.Status == "Hired"),
                Rejected = v.Applications.Count(a => a.Status == "Rejected"),
                AverageAiScore = v.Applications.Any(a => a.AiScore.HasValue)
                    ? Math.Round(v.Applications.Where(a => a.AiScore.HasValue).Average(a => a.AiScore!.Value), 1)
                    : 0
            }).ToList()
        };
    }
}
