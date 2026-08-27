namespace RecruitmentTracker.Services;

public interface ICvTextExtractor
{
    Task<string> ExtractAsync(string filePath, string extension);
}
