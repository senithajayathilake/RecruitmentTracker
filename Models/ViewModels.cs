using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RecruitmentTracker.Models;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(120)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class DashboardViewModel
{
    public int VacancyCount { get; set; }
    public int CandidateCount { get; set; }
    public int ApplicationCount { get; set; }
    public int ShortlistedCount { get; set; }
    public int PendingCount { get; set; }
    public int RejectedCount { get; set; }
    public int UpcomingInterviewCount { get; set; }
    public int PendingFeedbackCount { get; set; }

    // Sprint 2 - Recruitment Bottleneck Monitoring
    public int AwaitingInterviewCount { get; set; }
    public int OverdueFeedbackCount { get; set; }
    public int LongOpenApplicationCount { get; set; }

    public List<RecruitmentBottleneckViewModel> Bottlenecks { get; set; } = new();

    public List<Application> RecentApplications { get; set; } = new();
    public List<Interview> UpcomingInterviews { get; set; } = new();
}

public class RecruitmentBottleneckViewModel
{
    public string Type { get; set; } = string.Empty;

    public string CandidateName { get; set; } = string.Empty;

    public string VacancyTitle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int ApplicationId { get; set; }
}

public class CandidateFilterViewModel
{
    [Required]
    public int VacancyId { get; set; }
    public List<CandidateFilterResult> Results { get; set; } = new();
}

public class CandidateFilterResult
{
    public int CandidateId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public double Score { get; set; }
    public string MatchedSkills { get; set; } = string.Empty;
    public string MissingSkills { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

public class CvUploadViewModel
{
    public int CandidateId { get; set; }

    [Required]
    public IFormFile? File { get; set; }
}

public class InterviewScheduleViewModel
{
    public int ApplicationId { get; set; }

    [Required]
    [Display(Name = "Interview stage")]
    public int InterviewStageId { get; set; }

    [Required]
    [Display(Name = "Interviewer")]
    public string InterviewerId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Date and time")]
    public DateTime ScheduledAt { get; set; } = DateTime.Now.AddDays(1);

    [Range(15, 480)]
    [Display(Name = "Duration (minutes)")]
    public int DurationMinutes { get; set; } = 60;

    [StringLength(250)]
    [Display(Name = "Location / meeting link")]
    public string? LocationOrMeetingLink { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class InterviewFeedbackViewModel
{
    public int InterviewId { get; set; }

    [Range(1, 5)]
    [Display(Name = "Technical / role knowledge")]
    public int TechnicalScore { get; set; } = 3;

    [Range(1, 5)]
    [Display(Name = "Communication")]
    public int CommunicationScore { get; set; } = 3;

    [Range(1, 5)]
    [Display(Name = "Problem solving")]
    public int ProblemSolvingScore { get; set; } = 3;

    [Range(1, 5)]
    [Display(Name = "Culture / team fit")]
    public int CultureFitScore { get; set; } = 3;

    [Required, StringLength(30)]
    public string Recommendation { get; set; } = "Hold";

    [Required, StringLength(2000)]
    public string Comments { get; set; } = string.Empty;
}

public class InterviewerDashboardViewModel
{
    public List<Interview> Interviews { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new();
    public int PendingFeedbackCount { get; set; }
}

public class CandidateComparisonViewModel
{
    public List<Vacancy> Vacancies { get; set; } = new();

    public Vacancy? SelectedVacancy { get; set; }

    // All candidates available under the selected vacancy
    public List<CandidateComparisonRowViewModel> AvailableCandidates { get; set; } = new();

    // Application IDs selected by HR / Hiring Manager
    public List<int> SelectedApplicationIds { get; set; } = new();

    // Candidates actually displayed in the comparison
    public List<CandidateComparisonRowViewModel> Rows { get; set; } = new();
}

public class CandidateComparisonRowViewModel
{
    public int ApplicationId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public string Skills { get; set; } = string.Empty;
    public double AiScore { get; set; }
    public double? InterviewAverage { get; set; }
    public string LatestRecommendation { get; set; } = "No feedback";
    public string CurrentStage { get; set; } = "Not started";
    public string Status { get; set; } = string.Empty;
}

public class ReportsViewModel
{
    public int OpenVacancies { get; set; }
    public int TotalApplications { get; set; }
    public int Hired { get; set; }
    public int Rejected { get; set; }
    public int OnHold { get; set; }
    public int ScheduledInterviews { get; set; }
    public int CompletedInterviews { get; set; }
    public List<VacancyReportRowViewModel> VacancyRows { get; set; } = new();
}

public class VacancyReportRowViewModel
{
    public int VacancyId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Applications { get; set; }
    public int Hired { get; set; }
    public int Rejected { get; set; }
    public double AverageAiScore { get; set; }
}
