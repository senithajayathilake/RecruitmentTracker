using System.Text.RegularExpressions;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Services;

public class AiCvScreeningService : IAiCvScreeningService
{
    // Common skill aliases.
    // This prevents things like "ML" and "Machine Learning"
    // from being treated as completely different skills.
    private static readonly Dictionary<string, string[]> SkillAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["python"] = new[] { "python", "python3" },
            ["java"] = new[] { "java" },
            ["c#"] = new[] { "c#", "c sharp", "csharp" },
            ["c++"] = new[] { "c++", "cpp" },
            ["javascript"] = new[] { "javascript", "js" },
            ["typescript"] = new[] { "typescript", "ts" },

            ["machine learning"] = new[]
            {
                "machine learning",
                "ml",
                "machine-learning"
            },

            ["deep learning"] = new[]
            {
                "deep learning",
                "dl"
            },

            ["artificial intelligence"] = new[]
            {
                "artificial intelligence",
                "ai"
            },

            ["data analysis"] = new[]
            {
                "data analysis",
                "data analytics",
                "data analyst",
                "data analysis"
            },

            ["data science"] = new[]
            {
                "data science",
                "data scientist"
            },

            ["sql"] = new[]
            {
                "sql",
                "structured query language"
            },

            ["mysql"] = new[]
            {
                "mysql"
            },

            ["postgresql"] = new[]
            {
                "postgresql",
                "postgres",
                "postgre sql"
            },

            ["mongodb"] = new[]
            {
                "mongodb",
                "mongo db"
            },

            ["pandas"] = new[]
            {
                "pandas"
            },

            ["numpy"] = new[]
            {
                "numpy"
            },

            ["scikit-learn"] = new[]
            {
                "scikit-learn",
                "scikit learn",
                "sklearn"
            },

            ["tensorflow"] = new[]
            {
                "tensorflow",
                "tensor flow"
            },

            ["pytorch"] = new[]
            {
                "pytorch",
                "py torch"
            },

            ["nlp"] = new[]
            {
                "nlp",
                "natural language processing"
            },

            ["computer vision"] = new[]
            {
                "computer vision",
                "cv",
                "image processing"
            },

            ["react"] = new[]
            {
                "react",
                "react.js",
                "reactjs"
            },

            ["angular"] = new[]
            {
                "angular",
                "angular.js",
                "angularjs"
            },

            ["node.js"] = new[]
            {
                "node.js",
                "nodejs",
                "node js"
            },

            ["asp.net core"] = new[]
            {
                "asp.net core",
                "asp net core",
                "aspnet core"
            },

            ["git"] = new[]
            {
                "git",
                "github",
                "gitlab"
            },

            ["docker"] = new[]
            {
                "docker",
                "containerization",
                "containers"
            },

            ["azure"] = new[]
            {
                "azure",
                "microsoft azure"
            },

            ["aws"] = new[]
            {
                "aws",
                "amazon web services"
            },

            ["excel"] = new[]
            {
                "excel",
                "microsoft excel"
            },

            ["power bi"] = new[]
            {
                "power bi",
                "powerbi"
            },

            ["tableau"] = new[]
            {
                "tableau"
            },

            ["rest api"] = new[]
            {
                "rest api",
                "restful api",
                "rest apis",
                "web api"
            }
        };

    private static readonly string[] EducationKeywords =
    {
        "computer science",
        "software engineering",
        "information technology",
        "information systems",
        "data science",
        "artificial intelligence",
        "machine learning",
        "cyber security",
        "cybersecurity",
        "computer engineering",
        "statistics",
        "mathematics",
        "engineering"
    };

    public Task<AiScreeningResult> ScreenAsync(
        Candidate candidate,
        Vacancy vacancy,
        CandidateCv cv)
    {
        var cvText = cv.ExtractedText ?? string.Empty;

        var candidateText = Normalize(
            $"{candidate.FullName} " +
            $"{candidate.Skills} " +
            $"{candidate.HighestQualification} " +
            $"{candidate.YearsOfExperience} years " +
            $"{cvText}");

        var vacancyText = Normalize(
            $"{vacancy.JobTitle} " +
            $"{vacancy.Department} " +
            $"{vacancy.Description} " +
            $"{vacancy.Requirements}");

        // ---------------------------------------------------------
        // 1. Extract skills from the vacancy
        // ---------------------------------------------------------

        var requiredSkills = FindSkills(vacancyText);

        // ---------------------------------------------------------
        // 2. Find skills present in the CV
        // ---------------------------------------------------------

        var candidateSkills = FindSkills(candidateText);

        var matchedSkills = requiredSkills
            .Where(candidateSkills.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var missingSkills = requiredSkills
            .Where(skill => !candidateSkills.Contains(skill))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // ---------------------------------------------------------
        // 3. Skill score
        // ---------------------------------------------------------

        double skillScore;

        if (requiredSkills.Count == 0)
        {
            skillScore = 70;
        }
        else
        {
            skillScore =
                (double)matchedSkills.Count /
                requiredSkills.Count *
                100;
        }

        // ---------------------------------------------------------
        // 4. Experience score
        // ---------------------------------------------------------

        var requiredExperience =
            ExtractExperienceRequirement(vacancyText);

        double experienceScore;

        if (requiredExperience <= 0)
        {
            // If the vacancy does not specify experience,
            // don't punish the candidate.
            experienceScore = 85;
        }
        else if (candidate.YearsOfExperience >= requiredExperience)
        {
            experienceScore = 100;
        }
        else
        {
            experienceScore =
                (double)candidate.YearsOfExperience /
                requiredExperience *
                100;
        }

        // ---------------------------------------------------------
        // 5. Education score
        // ---------------------------------------------------------

        var educationScore =
            CalculateEducationScore(
                candidate.HighestQualification,
                vacancyText);

        // ---------------------------------------------------------
        // 6. Job relevance score
        // ---------------------------------------------------------

        var relevanceScore =
            CalculateRelevanceScore(
                vacancy,
                candidateText,
                candidateSkills);

        // ---------------------------------------------------------
        // 7. Additional skills score
        // ---------------------------------------------------------

        var additionalSkills =
            candidateSkills
                .Except(
                    requiredSkills,
                    StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();

        var additionalSkillsScore =
            additionalSkills.Count > 0 ? 100 : 60;

        // ---------------------------------------------------------
        // 8. Final weighted score
        // ---------------------------------------------------------

        var finalScore =
            skillScore * 0.40 +
            experienceScore * 0.25 +
            educationScore * 0.15 +
            relevanceScore * 0.15 +
            additionalSkillsScore * 0.05;

        finalScore = Math.Clamp(
            Math.Round(finalScore, 1),
            0,
            100);

        // ---------------------------------------------------------
        // 9. Recommendation
        // ---------------------------------------------------------

        var recommendation =
            finalScore >= 85
                ? "Strong shortlist"
                : finalScore >= 70
                    ? "Shortlist"
                    : finalScore >= 55
                        ? "Review"
                        : "Low match";

        // ---------------------------------------------------------
        // 10. Generate explanation
        // ---------------------------------------------------------

        var summary =
            BuildSummary(
                finalScore,
                matchedSkills,
                missingSkills,
                experienceScore,
                educationScore,
                relevanceScore,
                candidate.YearsOfExperience,
                requiredExperience);

        return Task.FromResult(
            new AiScreeningResult
            {
                Score = finalScore,

                MatchedSkills =
                    string.Join(", ", matchedSkills),

                MissingSkills =
                    string.Join(", ", missingSkills),

                Summary = summary,

                Recommendation =
                    recommendation
            });
    }

    // =========================================================
    // SKILL DETECTION
    // =========================================================

    private static HashSet<string> FindSkills(string text)
    {
        var result =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var skill in SkillAliases)
        {
            foreach (var alias in skill.Value)
            {
                if (ContainsPhrase(text, alias))
                {
                    result.Add(skill.Key);
                    break;
                }
            }
        }

        return result;
    }

    private static bool ContainsPhrase(
        string text,
        string phrase)
    {
        if (string.IsNullOrWhiteSpace(text) ||
            string.IsNullOrWhiteSpace(phrase))
        {
            return false;
        }

        var escaped =
            Regex.Escape(
                Normalize(phrase));

        return Regex.IsMatch(
            text,
            $@"(?<![a-z0-9]){escaped}(?![a-z0-9])",
            RegexOptions.IgnoreCase);
    }

   

    private static int ExtractExperienceRequirement(
        string text)
    {
        var patterns = new[]
        {
            @"(\d+)\s*\+?\s*(?:years?|yrs?)\s*(?:of)?\s*experience",

            @"experience\s*(?:of|:)?\s*(\d+)\s*\+?\s*(?:years?|yrs?)",

            @"minimum\s*(?:of)?\s*(\d+)\s*\+?\s*(?:years?|yrs?)",

            @"at\s*least\s*(\d+)\s*\+?\s*(?:years?|yrs?)"
        };

        foreach (var pattern in patterns)
        {
            var match =
                Regex.Match(
                    text,
                    pattern,
                    RegexOptions.IgnoreCase);

            if (match.Success &&
                int.TryParse(
                    match.Groups[1].Value,
                    out var years))
            {
                return years;
            }
        }

        return 0;
    }

    // =========================================================
    // EDUCATION
    // =========================================================

    private static double CalculateEducationScore(
        string? qualification,
        string vacancyText)
    {
        if (string.IsNullOrWhiteSpace(qualification))
        {
            return 50;
        }

        var qualificationText =
            Normalize(qualification);

        var relevant =
            EducationKeywords.Any(
                keyword =>
                    ContainsPhrase(
                        qualificationText,
                        keyword));

        // If the qualification is relevant to the general
        // vacancy domain, give a stronger score.
        if (relevant)
        {
            return 100;
        }

        // Candidate has a qualification, but it isn't clearly
        // related to the job.
        return 75;
    }

    // =========================================================
    // JOB RELEVANCE
    // =========================================================

    private static double CalculateRelevanceScore(
        Vacancy vacancy,
        string candidateText,
        HashSet<string> candidateSkills)
    {
        var titleWords =
            GetImportantWords(vacancy.JobTitle);

        var descriptionWords =
            GetImportantWords(
                vacancy.Description);

        var requirementWords =
            GetImportantWords(
                vacancy.Requirements);

        var titleMatches =
            titleWords.Count == 0
                ? 0
                : titleWords.Count(
                    word =>
                        ContainsPhrase(
                            candidateText,
                            word));

        var descriptionMatches =
            descriptionWords.Count == 0
                ? 0
                : descriptionWords.Count(
                    word =>
                        ContainsPhrase(
                            candidateText,
                            word));

        var requirementMatches =
            requirementWords.Count == 0
                ? 0
                : requirementWords.Count(
                    word =>
                        ContainsPhrase(
                            candidateText,
                            word));

        var titleScore =
            titleWords.Count == 0
                ? 70
                : (double)titleMatches /
                  titleWords.Count *
                  100;

        var descriptionScore =
            descriptionWords.Count == 0
                ? 70
                : (double)descriptionMatches /
                  descriptionWords.Count *
                  100;

        var requirementScore =
            requirementWords.Count == 0
                ? 70
                : (double)requirementMatches /
                  requirementWords.Count *
                  100;

        var skillBonus =
            Math.Min(
                20,
                candidateSkills.Count * 2);

        var score =
            titleScore * 0.35 +
            descriptionScore * 0.25 +
            requirementScore * 0.25 +
            skillBonus * 0.15;

        return Math.Clamp(
            score,
            0,
            100);
    }

    // =========================================================
    // IMPORTANT WORDS
    // =========================================================

    private static List<string> GetImportantWords(
        string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        var stopWords =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                "and",
                "or",
                "the",
                "a",
                "an",
                "with",
                "for",
                "to",
                "of",
                "in",
                "on",
                "at",
                "is",
                "are",
                "be",
                "this",
                "that",
                "from",
                "as",
                "by",
                "will",
                "must",
                "have",
                "has",
                "years",
                "year",
                "experience",
                "knowledge",
                "skills",
                "skill",
                "required",
                "requirements",
                "responsibilities",
                "candidate",
                "job",
                "role",
                "work",
                "working",
                "team",
                "ability",
                "strong",
                "good",
                "basic",
                "understanding",
                "familiarity"
            };

        return Regex
            .Split(
                text.ToLowerInvariant(),
                @"[^a-z0-9+#.]+")
            .Where(
                word =>
                    word.Length >= 3 &&
                    !stopWords.Contains(word))
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .Take(100)
            .ToList();
    }

    // =========================================================
    // SUMMARY
    // =========================================================

    private static string BuildSummary(
        double score,
        List<string> matched,
        List<string> missing,
        double experienceScore,
        double educationScore,
        double relevanceScore,
        int candidateExperience,
        int requiredExperience)
    {
        var experienceText =
            requiredExperience <= 0
                ? "The vacancy does not specify a minimum experience requirement."
                : candidateExperience >= requiredExperience
                    ? $"The candidate meets the required experience of {requiredExperience} year(s)."
                    : $"The candidate has {candidateExperience} year(s) of experience compared with the required {requiredExperience} year(s).";

        var skillText =
            matched.Count > 0
                ? $"The candidate matches {matched.Count} identified job skill(s)"
                : "The candidate has limited matches against the identified job skills";

        var missingText =
            missing.Count > 0
                ? $"and is missing {missing.Count} identified skill(s)."
                : "and no major identified skills are missing.";

        var overall =
            score >= 85
                ? "Overall, the candidate is a strong match."
                : score >= 70
                    ? "Overall, the candidate is a good match and should be considered."
                    : score >= 55
                        ? "Overall, the candidate may require manual review."
                        : "Overall, the candidate has limited alignment with the vacancy.";

        return
            $"{skillText} {missingText} " +
            $"{experienceText} " +
            $"Education match: {educationScore:0}%. " +
            $"Job relevance: {relevanceScore:0}%. " +
            overall;
    }

    // =========================================================
    // TEXT NORMALIZATION
    // =========================================================

    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        text =
            text.ToLowerInvariant();

        text =
            text.Replace("–", "-")
                .Replace("—", "-")
                .Replace("’", "'");

        text =
            Regex.Replace(
                text,
                @"\s+",
                " ");

        return text.Trim();
    }
}