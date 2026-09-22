using System.Text.RegularExpressions;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Services;

public class AiCvScreeningService : IAiCvScreeningService
{
    // =========================================================
    // SKILL ALIASES
    // =========================================================

    private static readonly Dictionary<string, string[]> SkillAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["python"] = new[]
            {
                "python",
                "python3"
            },

            ["java"] = new[]
            {
                "java"
            },

            ["c#"] = new[]
            {
                "c#",
                "c sharp",
                "csharp"
            },

            ["c++"] = new[]
            {
                "c++",
                "cpp"
            },

            ["javascript"] = new[]
            {
                "javascript",
                "js"
            },

            ["typescript"] = new[]
            {
                "typescript",
                "ts"
            },

            ["machine learning"] = new[]
            {
                "machine learning",
                "machine-learning",
                "ml"
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
                "data analyst"
            },

            ["data science"] = new[]
            {
                "data science",
                "data scientist"
            },

            ["sql"] = new[]
            {
                "sql",
                "sql server",
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
                "microsoft azure",
                "azure devops"
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


    // =========================================================
    // MAIN SCREENING METHOD
    // =========================================================

    public Task<AiScreeningResult> ScreenAsync(
        Candidate candidate,
        Vacancy vacancy,
        CandidateCv cv)
    {
        var cvText =
            cv.ExtractedText ?? string.Empty;


        // =====================================================
        // CANDIDATE EXPERIENCE
        // =====================================================

        // Prefer candidate profile data if available.
        // Otherwise fall back to information extracted from CV.

        var candidateExperience =
            candidate.YearsOfExperience > 0
                ? candidate.YearsOfExperience
                : ExtractCandidateExperience(cvText);


        // =====================================================
        // CANDIDATE EDUCATION
        // =====================================================

        var detectedEducationFields =
            DetectEducationFields(cvText);


        var candidateQualification =
            !string.IsNullOrWhiteSpace(
                candidate.HighestQualification)
                ? candidate.HighestQualification
                : string.Join(
                    ", ",
                    detectedEducationFields);


        // =====================================================
        // BUILD CANDIDATE TEXT
        // =====================================================

        var candidateText =
            Normalize(
                $"{candidate.FullName} " +
                $"{candidate.Skills} " +
                $"{candidateQualification} " +
                $"{candidateExperience} years " +
                $"{cvText}");


        // =====================================================
        // BUILD VACANCY TEXT
        // =====================================================

        var vacancyText =
            Normalize(
                $"{vacancy.JobTitle} " +
                $"{vacancy.Department} " +
                $"{vacancy.Description} " +
                $"{vacancy.Requirements}");


        // =====================================================
        // 1. REQUIRED SKILLS
        // =====================================================

        var requiredSkills =
            FindSkills(
                vacancyText);


        // =====================================================
        // 2. CANDIDATE SKILLS
        // =====================================================

        var candidateSkills =
            FindSkills(
                candidateText);


        var matchedSkills =
            requiredSkills
                .Where(
                    candidateSkills.Contains)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();


        var missingSkills =
            requiredSkills
                .Where(
                    skill =>
                        !candidateSkills.Contains(skill))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();


        // =====================================================
        // 3. SKILL MATCH SCORE
        // =====================================================

        double skillScore;


        if (requiredSkills.Count == 0)
        {
            // No specific technical skills were entered
            // by HR, so use a neutral score.

            skillScore = 70;
        }
        else
        {
            skillScore =
                (double)matchedSkills.Count /
                requiredSkills.Count *
                100;
        }


        // =====================================================
        // 4. EXPERIENCE SCORE
        // =====================================================

        var requiredExperience =
            ExtractExperienceRequirement(
                vacancyText);


        double experienceScore;


        if (requiredExperience <= 0)
        {
            // Do not punish candidates when the vacancy
            // has no minimum experience requirement.

            experienceScore = 85;
        }
        else if (
            candidateExperience >=
            requiredExperience)
        {
            experienceScore = 100;
        }
        else
        {
            experienceScore =
                (double)candidateExperience /
                requiredExperience *
                100;
        }


        // =====================================================
        // 5. EDUCATION SCORE
        // =====================================================

        var educationScore =
            CalculateEducationScore(
                candidate.HighestQualification,
                cvText,
                vacancyText);


        // =====================================================
        // 6. GENERAL JOB RELEVANCE
        // =====================================================

        var relevanceScore =
            CalculateRelevanceScore(
                vacancy,
                candidateText,
                candidateSkills);


        // =====================================================
        // 7. CV ↔ JOB TEXT SIMILARITY
        // =====================================================

        var textSimilarityScore =
            CalculateTextSimilarity(
                cvText,
                vacancyText);


        // =====================================================
        // 8. ADDITIONAL SKILLS
        // =====================================================

        var additionalSkills =
            candidateSkills
                .Except(
                    requiredSkills,
                    StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();


        var additionalSkillsScore =
            additionalSkills.Count > 0
                ? 100
                : 60;


        // =====================================================
        // 9. FINAL WEIGHTED SCORE
        // =====================================================

        /*
            Skills match            35%
            CV-job similarity       25%
            Experience              15%
            Education               10%
            General job relevance   10%
            Additional skills        5%

            TOTAL                  100%
        */

        var finalScore =
            skillScore * 0.35 +
            textSimilarityScore * 0.25 +
            experienceScore * 0.15 +
            educationScore * 0.10 +
            relevanceScore * 0.10 +
            additionalSkillsScore * 0.05;


        finalScore =
            Math.Clamp(
                Math.Round(
                    finalScore,
                    1),
                0,
                100);


        // =====================================================
        // 10. RECOMMENDATION
        // =====================================================

        var recommendation =
            finalScore >= 85
                ? "Strong shortlist"
                : finalScore >= 70
                    ? "Shortlist"
                    : finalScore >= 55
                        ? "Review"
                        : "Low match";


        // =====================================================
        // 11. EXPLANATION
        // =====================================================

        var summary =
            BuildSummary(
                finalScore,
                matchedSkills,
                missingSkills,
                skillScore,
                textSimilarityScore,
                experienceScore,
                educationScore,
                relevanceScore,
                candidateExperience,
                requiredExperience);


        // =====================================================
        // DEBUG INFORMATION
        // =====================================================

        System.Diagnostics.Debug.WriteLine(
            $"AI SCREENING DEBUG | " +
            $"CV: {cv.FileName} | " +
            $"CV characters: {cvText.Length} | " +
            $"Experience detected: {candidateExperience} | " +
            $"Education: {educationScore:0}% | " +
            $"Skills: {skillScore:0}% | " +
            $"Similarity: {textSimilarityScore:0}% | " +
            $"Final score: {finalScore:0.0}%");


        // =====================================================
        // RETURN RESULT
        // =====================================================

        return Task.FromResult(
            new AiScreeningResult
            {
                Score =
                    finalScore,

                MatchedSkills =
                    string.Join(
                        ", ",
                        matchedSkills),

                MissingSkills =
                    string.Join(
                        ", ",
                        missingSkills),

                Summary =
                    summary,

                Recommendation =
                    recommendation
            });
    }


    // =========================================================
    // SKILL DETECTION
    // =========================================================

    private static HashSet<string> FindSkills(
        string text)
    {
        var result =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);


        foreach (var skill in SkillAliases)
        {
            foreach (var alias in skill.Value)
            {
                if (
                    ContainsPhrase(
                        text,
                        alias))
                {
                    result.Add(
                        skill.Key);

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
        if (
            string.IsNullOrWhiteSpace(text) ||
            string.IsNullOrWhiteSpace(phrase))
        {
            return false;
        }


        var escaped =
            Regex.Escape(
                Normalize(phrase));


        return Regex.IsMatch(
            Normalize(text),
            $@"(?<![a-z0-9]){escaped}(?![a-z0-9])",
            RegexOptions.IgnoreCase);
    }


    // =========================================================
    // VACANCY EXPERIENCE REQUIREMENT
    // =========================================================

    private static int ExtractExperienceRequirement(
        string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }


        var normalized =
            Normalize(text);


        var numberPattern =
            @"(\d+|one|two|three|four|five|six|seven|eight|nine|ten)";


        var patterns =
            new[]
            {
                $@"{numberPattern}\s*\+?\s*(?:years?|yrs?)\s*(?:of\s+)?experience",

                $@"experience\s*(?:of|:)?\s*{numberPattern}\s*\+?\s*(?:years?|yrs?)",

                $@"minimum\s*(?:of\s+)?{numberPattern}\s*\+?\s*(?:years?|yrs?)",

                $@"at\s*least\s*{numberPattern}\s*\+?\s*(?:years?|yrs?)"
            };


        foreach (var pattern in patterns)
        {
            var match =
                Regex.Match(
                    normalized,
                    pattern,
                    RegexOptions.IgnoreCase);


            if (match.Success)
            {
                var years =
                    ParseExperienceNumber(
                        match.Groups[1].Value);


                if (years > 0)
                {
                    return years;
                }
            }
        }


        return 0;
    }


    // =========================================================
    // CANDIDATE EXPERIENCE FROM CV
    // =========================================================

    private static int ExtractCandidateExperience(
        string cvText)
    {
        if (string.IsNullOrWhiteSpace(cvText))
        {
            return 0;
        }


        var normalized =
            Normalize(cvText);


        var numberPattern =
            @"(\d+|one|two|three|four|five|six|seven|eight|nine|ten)";


        var patterns =
            new[]
            {
                $@"{numberPattern}\s*\+?\s*(?:years?|yrs?)\s*(?:of\s+)?(?:professional\s+)?experience",

                $@"{numberPattern}\s*\+?\s*(?:years?|yrs?)\s+(?:professional\s+)?experience",

                $@"experience\s*(?:of|:)?\s*{numberPattern}\s*\+?\s*(?:years?|yrs?)"
            };


        foreach (var pattern in patterns)
        {
            var match =
                Regex.Match(
                    normalized,
                    pattern,
                    RegexOptions.IgnoreCase);


            if (match.Success)
            {
                var years =
                    ParseExperienceNumber(
                        match.Groups[1].Value);


                if (years > 0)
                {
                    return years;
                }
            }
        }


        return 0;
    }


    private static int ParseExperienceNumber(
        string value)
    {
        if (
            int.TryParse(
                value,
                out var numericValue))
        {
            return numericValue;
        }


        return value
            .Trim()
            .ToLowerInvariant()
            switch
        {
            "one" => 1,
            "two" => 2,
            "three" => 3,
            "four" => 4,
            "five" => 5,
            "six" => 6,
            "seven" => 7,
            "eight" => 8,
            "nine" => 9,
            "ten" => 10,
            _ => 0
        };
    }


    // =========================================================
    // EDUCATION FIELD DETECTION
    // =========================================================

    private static List<string> DetectEducationFields(
        string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }


        var normalized =
            Normalize(text);


        // This version removes spaces/punctuation.
        // It helps when PDF extraction produces text such as:
        //
        // "SoftwareEngineering"
        //
        // instead of:
        //
        // "Software Engineering"

        var compact =
            Regex.Replace(
                normalized,
                @"[^a-z0-9]",
                "");


        var fields =
            new List<string>();


        if (
            normalized.Contains(
                "software engineering") ||
            compact.Contains(
                "softwareengineering"))
        {
            fields.Add(
                "software engineering");
        }


        if (
            normalized.Contains(
                "computer science") ||
            compact.Contains(
                "computerscience"))
        {
            fields.Add(
                "computer science");
        }


        if (
            normalized.Contains(
                "information technology") ||
            compact.Contains(
                "informationtechnology"))
        {
            fields.Add(
                "information technology");
        }


        if (
            normalized.Contains(
                "information systems") ||
            compact.Contains(
                "informationsystems"))
        {
            fields.Add(
                "information systems");
        }


        if (
            normalized.Contains(
                "artificial intelligence") ||
            compact.Contains(
                "artificialintelligence"))
        {
            fields.Add(
                "artificial intelligence");
        }


        if (
            normalized.Contains(
                "data science") ||
            compact.Contains(
                "datascience"))
        {
            fields.Add(
                "data science");
        }


        if (
            normalized.Contains(
                "machine learning") ||
            compact.Contains(
                "machinelearning"))
        {
            fields.Add(
                "machine learning");
        }


        if (
            normalized.Contains(
                "computer engineering") ||
            compact.Contains(
                "computerengineering"))
        {
            fields.Add(
                "computer engineering");
        }


        if (
            normalized.Contains(
                "cyber security") ||
            normalized.Contains(
                "cybersecurity") ||
            compact.Contains(
                "cybersecurity"))
        {
            fields.Add(
                "cyber security");
        }


        if (
            normalized.Contains(
                "statistics") ||
            compact.Contains(
                "statistics"))
        {
            fields.Add(
                "statistics");
        }


        if (
            normalized.Contains(
                "mathematics") ||
            compact.Contains(
                "mathematics"))
        {
            fields.Add(
                "mathematics");
        }


        return fields
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }


    // =========================================================
    // EDUCATION SCORE
    // =========================================================

    private static double CalculateEducationScore(
        string? profileQualification,
        string cvText,
        string vacancyText)
    {
        // Use both profile information AND uploaded CV.

        var candidateEducationText =
            $"{profileQualification} {cvText}";


        var candidateFields =
            DetectEducationFields(
                candidateEducationText);


        var vacancyFields =
            DetectEducationFields(
                vacancyText);


        System.Diagnostics.Debug.WriteLine(
            $"EDUCATION DEBUG | " +
            $"Candidate fields: " +
            $"{string.Join(", ", candidateFields)} | " +
            $"Vacancy fields: " +
            $"{string.Join(", ", vacancyFields)}");


        // No recognizable relevant education
        // was found.

        if (candidateFields.Count == 0)
        {
            return 50;
        }


        // Candidate has a relevant technical degree,
        // but vacancy does not specify a particular
        // discipline.

        if (vacancyFields.Count == 0)
        {
            return 90;
        }


        // Check whether candidate's degree matches one of
        // the disciplines accepted by the vacancy.

        var directMatch =
            candidateFields.Any(
                candidateField =>
                    vacancyFields.Contains(
                        candidateField,
                        StringComparer.OrdinalIgnoreCase));


        if (directMatch)
        {
            return 100;
        }


        // Candidate still has a relevant computing /
        // technical qualification.

        return 85;
    }


    // =========================================================
    // GENERAL JOB RELEVANCE
    // =========================================================

    private static double CalculateRelevanceScore(
        Vacancy vacancy,
        string candidateText,
        HashSet<string> candidateSkills)
    {
        var titleWords =
            GetImportantWords(
                vacancy.JobTitle);


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
                Normalize(text),
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
    // CV ↔ JOB TEXT SIMILARITY
    // =========================================================

    private static double CalculateTextSimilarity(
        string cvText,
        string vacancyText)
    {
        if (
            string.IsNullOrWhiteSpace(cvText) ||
            string.IsNullOrWhiteSpace(vacancyText))
        {
            return 0;
        }


        var cvWords =
            GetWordFrequencies(
                cvText);


        var vacancyWords =
            GetWordFrequencies(
                vacancyText);


        if (
            cvWords.Count == 0 ||
            vacancyWords.Count == 0)
        {
            return 0;
        }


        var allWords =
            cvWords.Keys
                .Union(
                    vacancyWords.Keys)
                .ToList();


        double dotProduct = 0;
        double cvMagnitude = 0;
        double vacancyMagnitude = 0;


        foreach (var word in allWords)
        {
            var cvCount =
                cvWords.TryGetValue(
                    word,
                    out var cvValue)
                    ? cvValue
                    : 0;


            var vacancyCount =
                vacancyWords.TryGetValue(
                    word,
                    out var vacancyValue)
                    ? vacancyValue
                    : 0;


            dotProduct +=
                cvCount *
                vacancyCount;


            cvMagnitude +=
                cvCount *
                cvCount;


            vacancyMagnitude +=
                vacancyCount *
                vacancyCount;
        }


        if (
            cvMagnitude == 0 ||
            vacancyMagnitude == 0)
        {
            return 0;
        }


        var similarity =
            dotProduct /
            (
                Math.Sqrt(cvMagnitude) *
                Math.Sqrt(vacancyMagnitude)
            );


        return Math.Clamp(
            similarity * 100,
            0,
            100);
    }


    // =========================================================
    // WORD FREQUENCY
    // =========================================================

    private static Dictionary<string, int>
        GetWordFrequencies(
            string text)
    {
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
                "candidate",
                "job",
                "role"
            };


        return Regex
            .Split(
                Normalize(text),
                @"[^a-z0-9+#.]+")
            .Where(
                word =>
                    word.Length >= 2 &&
                    !stopWords.Contains(word))
            .GroupBy(
                word => word,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase);
    }


    // =========================================================
    // SUMMARY
    // =========================================================

    private static string BuildSummary(
        double score,
        List<string> matched,
        List<string> missing,
        double skillScore,
        double textSimilarityScore,
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
            $"Skills match: {skillScore:0}%. " +
            $"CV-to-job similarity: {textSimilarityScore:0}%. " +
            $"{experienceText} " +
            $"Experience match: {experienceScore:0}%. " +
            $"Education match: {educationScore:0}%. " +
            $"Job relevance: {relevanceScore:0}%. " +
            overall;
    }


    // =========================================================
    // TEXT NORMALIZATION
    // =========================================================

    private static string Normalize(
        string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }


        text =
            text.ToLowerInvariant();


        text =
            text.Replace(
                    "–",
                    "-")
                .Replace(
                    "—",
                    "-")
                .Replace(
                    "’",
                    "'");


        text =
            Regex.Replace(
                text,
                @"\s+",
                " ");


        return text.Trim();
    }
}