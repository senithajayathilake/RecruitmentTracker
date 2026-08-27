using RecruitmentTracker.Models;

namespace RecruitmentTracker.Services;

public interface IAiCvScreeningService
{
    Task<AiScreeningResult> ScreenAsync(Candidate candidate, Vacancy vacancy, CandidateCv cv);
}

public class AiScreeningResult
{
    public double Score { get; set; }
    public string MatchedSkills { get; set; } = string.Empty;
    public string MissingSkills { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}
