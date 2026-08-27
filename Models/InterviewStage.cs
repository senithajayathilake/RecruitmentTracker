using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models;

public class InterviewStage
{
    public int InterviewStageId { get; set; }

    public int VacancyId { get; set; }
    public Vacancy? Vacancy { get; set; }

    [Required, StringLength(100)]
    public string StageName { get; set; } = string.Empty;

    [Range(1, 50)]
    public int StageOrder { get; set; } = 1;

    [StringLength(500)]
    public string? Description { get; set; }
}
