using RecruitmentTracker.Models;

namespace RecruitmentTracker.Services;

public class CandidateFilterService : ICandidateFilterService
{
    private readonly IAiCvScreeningService _ai;

    public CandidateFilterService(IAiCvScreeningService ai) => _ai = ai;

    public async Task<List<CandidateFilterResult>> FilterAsync(Vacancy vacancy, List<Candidate> candidates)
    {
        var results = new List<CandidateFilterResult>();

        foreach (var candidate in candidates)
        {
            var cv = candidate.Cvs.OrderByDescending(x => x.UploadedDate).FirstOrDefault();
            if (cv == null)
            {
                results.Add(new CandidateFilterResult
                {
                    CandidateId = candidate.CandidateId,
                    CandidateName = candidate.FullName,
                    Email = candidate.Email,
                    Score = 0,
                    MatchedSkills = "",
                    MissingSkills = vacancy.Requirements,
                    Status = candidate.Status,
                    Recommendation = "CV required"
                });
                continue;
            }

            var result = await _ai.ScreenAsync(candidate, vacancy, cv);
            cv.AiScore = result.Score;
            cv.MatchedSkills = result.MatchedSkills;
            cv.MissingSkills = result.MissingSkills;
            cv.AiSummary = result.Summary;

            results.Add(new CandidateFilterResult
            {
                CandidateId = candidate.CandidateId,
                CandidateName = candidate.FullName,
                Email = candidate.Email,
                Score = result.Score,
                MatchedSkills = result.MatchedSkills,
                MissingSkills = result.MissingSkills,
                Status = candidate.Status,
                Recommendation = result.Recommendation
            });
        }

        return results.OrderByDescending(x => x.Score).ToList();
    }
}
