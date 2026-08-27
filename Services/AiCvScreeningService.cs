using System.Text.RegularExpressions;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Services;

// A local, explainable AI-style screening engine. It works without an external API,
// so the project runs immediately. It uses weighted requirement matching, CV
// experience/education signals, and a confidence-based recommendation.
public class AiCvScreeningService : IAiCvScreeningService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and","or","the","a","an","with","for","to","of","in","on","at","is","are",
        "be","this","that","from","as","by","will","must","have","years","year",
        "experience","knowledge","skills","skill","required","responsibilities",
        "candidate","job","role","work","working","team"
    };

    public Task<AiScreeningResult> ScreenAsync(Candidate candidate, Vacancy vacancy, CandidateCv cv)
    {
        var requirementText = $"{vacancy.Requirements} {vacancy.Description}";
        var required = Tokenize(requirementText);
        var cvText = $"{candidate.FullName} {candidate.Skills} {candidate.HighestQualification} " +
                     $"{candidate.YearsOfExperience} years {cv.ExtractedText}";
        var candidateTokens = Tokenize(cvText);

        var matched = required.Where(candidateTokens.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var missing = required.Where(x => !candidateTokens.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var skillScore = required.Count == 0 ? 0 : (double)matched.Count / required.Count * 100;

        var experienceRequired = ExtractExperienceRequirement(requirementText);
        var experienceScore = experienceRequired <= 0
            ? 100
            : Math.Min(100, (double)candidate.YearsOfExperience / experienceRequired * 100);

        var educationScore = string.IsNullOrWhiteSpace(candidate.HighestQualification) ? 60 : 100;

        // Weighted model: requirements are the strongest signal.
        var score = Math.Round(skillScore * 0.70 + experienceScore * 0.20 + educationScore * 0.10, 1);
        score = Math.Clamp(score, 0, 100);

        var recommendation = score >= 80 ? "Strong shortlist"
            : score >= 65 ? "Review / shortlist"
            : score >= 50 ? "Manual review"
            : "Low match";

        var summary = $"The screening model found {matched.Count} matching requirement terms " +
                      $"and {missing.Count} missing terms. Estimated experience match: {experienceScore:0}%. " +
                      $"Overall recommendation: {recommendation}.";

        return Task.FromResult(new AiScreeningResult
        {
            Score = score,
            MatchedSkills = string.Join(", ", matched.Take(30)),
            MissingSkills = string.Join(", ", missing.Take(30)),
            Summary = summary,
            Recommendation = recommendation
        });
    }

    private static HashSet<string> Tokenize(string text)
    {
        return Regex.Split(text.ToLowerInvariant(), @"[^a-z0-9+#.]+")
            .Where(x => x.Length >= 2 && !StopWords.Contains(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static int ExtractExperienceRequirement(string text)
    {
        var match = Regex.Match(text, @"(?:at least|minimum of|min\.?)?\s*(\d+)\s*\+?\s*(?:years?|yrs?)\s*(?:of)?\s*experience",
            RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out var years) ? years : 0;
    }
}
