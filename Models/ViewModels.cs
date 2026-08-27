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
    public List<Application> RecentApplications { get; set; } = new();
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
