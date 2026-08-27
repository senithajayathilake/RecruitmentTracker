using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models;

public class Vacancy
{
    public int VacancyId { get; set; }

    [Required, StringLength(120)]
    public string JobTitle { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Department { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Location { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string EmploymentType { get; set; } = "Full Time";

    [Range(0, 999999999)]
    public decimal Salary { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Requirements { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int NumberOfPositions { get; set; } = 1;

    [DataType(DataType.Date)]
    public DateTime ApplicationDeadline { get; set; } = DateTime.Today.AddDays(30);

    public string Status { get; set; } = "Open";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser? CreatedByUser { get; set; }

    public ICollection<InterviewStage> InterviewStages { get; set; } = new List<InterviewStage>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
