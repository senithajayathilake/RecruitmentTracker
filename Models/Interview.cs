using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models;

public class Interview
{
    public int InterviewId { get; set; }

    public int ApplicationId { get; set; }
    public Application? Application { get; set; }

    public int InterviewStageId { get; set; }
    public InterviewStage? InterviewStage { get; set; }

    [Required]
    public string InterviewerId { get; set; } = string.Empty;
    public ApplicationUser? Interviewer { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; } = DateTime.Now.AddDays(1);

    [Range(15, 480)]
    public int DurationMinutes { get; set; } = 60;

    [StringLength(250)]
    [Display(Name = "Location / meeting link")]
    public string? LocationOrMeetingLink { get; set; }

    [Required, StringLength(30)]
    public string Status { get; set; } = "Scheduled";

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public InterviewFeedback? Feedback { get; set; }
}
