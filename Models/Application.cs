using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models;

public class Application
{
    public int ApplicationId { get; set; }

    public int CandidateId { get; set; }
    public Candidate? Candidate { get; set; }

    public int VacancyId { get; set; }
    public Vacancy? Vacancy { get; set; }

    public int CandidateCvId { get; set; }
    public CandidateCv? CandidateCv { get; set; }

    [Required, StringLength(40)]
    public string Status { get; set; } = "New";

    public DateTime AppliedDate { get; set; } = DateTime.UtcNow;

    public double? AiScore { get; set; }

    public string? AiSummary { get; set; }

    public string? MatchedSkills { get; set; }

    public string? MissingSkills { get; set; }
}
