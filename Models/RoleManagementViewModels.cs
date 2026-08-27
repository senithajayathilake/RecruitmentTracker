using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models;

public class RoleAssignmentViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Candidate";
}

public class UserRoleRowViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class CandidateDashboardViewModel
{
    public Candidate Candidate { get; set; } = new();
    public List<CandidateCv> Cvs { get; set; } = new();
    public List<Vacancy> OpenVacancies { get; set; } = new();
    public List<Application> Applications { get; set; } = new();
}

public class HiringManagerDashboardViewModel
{
    public List<Application> Applications { get; set; } = new();
}
