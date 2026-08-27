using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models;

public class CandidateCv
{
    public int CandidateCvId { get; set; }

    public int CandidateId { get; set; }
    public Candidate? Candidate { get; set; }

    [Required, StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    public string ExtractedText { get; set; } = string.Empty;

    public double? AiScore { get; set; }
    public string? MatchedSkills { get; set; }
    public string? MissingSkills { get; set; }
    public string? AiSummary { get; set; }

    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
