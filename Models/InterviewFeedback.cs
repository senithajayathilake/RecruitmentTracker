using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentTracker.Models;

public class InterviewFeedback
{
    public int InterviewFeedbackId { get; set; }

    public int InterviewId { get; set; }
    public Interview? Interview { get; set; }

    [Required]
    public string InterviewerId { get; set; } = string.Empty;
    public ApplicationUser? Interviewer { get; set; }

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

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public double AverageScore =>
        Math.Round((TechnicalScore + CommunicationScore + ProblemSolvingScore + CultureFitScore) / 4.0, 1);
}
