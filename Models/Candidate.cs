using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models;

public class Candidate
{
    public int CandidateId { get; set; }

    // Links the recruitment profile to the ASP.NET Identity account.
    public string? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }

    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(40)]
    public string? Phone { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(150)]
    public string? HighestQualification { get; set; }

    [Range(0, 60)]
    public int YearsOfExperience { get; set; }

    [StringLength(1000)]
    public string? Skills { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Added";

    public ICollection<CandidateCv> Cvs { get; set; } = new List<CandidateCv>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
