using RecruitmentTracker.Models;

namespace RecruitmentTracker.Services;

public interface ICandidateFilterService
{
    Task<List<CandidateFilterResult>> FilterAsync(Vacancy vacancy, List<Candidate> candidates);
}
