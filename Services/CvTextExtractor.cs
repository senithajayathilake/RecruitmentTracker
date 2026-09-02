using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace RecruitmentTracker.Services;

public class CvTextExtractor : ICvTextExtractor
{
    public Task<string> ExtractAsync(string filePath, string extension)
    {
        extension = extension.ToLowerInvariant();

        try
        {
            string text = extension switch
            {
                ".pdf" => ExtractPdf(filePath),
                ".docx" => ExtractDocx(filePath),
                _ => string.Empty
            };

            return Task.FromResult(text);
        }
        catch (Exception ex)
        {
            // Make the extraction failure visible in Visual Studio
            System.Diagnostics.Debug.WriteLine(
                $"CV EXTRACTION ERROR: {ex.Message}");

            return Task.FromResult(string.Empty);
        }
    }

    private static string ExtractPdf(string path)
    {
        using var document = PdfDocument.Open(path);

        var pages = document.GetPages().ToList();

        var text = string.Join(
            Environment.NewLine,
            pages.Select(page => page.Text)
        );

        System.Diagnostics.Debug.WriteLine(
            $"CV EXTRACTION: {text.Length} characters extracted from {pages.Count} page(s).");

        return text;
    }

    private static string ExtractDocx(string path)
    {
        using var doc = WordprocessingDocument.Open(path, false);

        var body = doc.MainDocumentPart?.Document.Body;

        var text = body?.InnerText ?? string.Empty;

        System.Diagnostics.Debug.WriteLine(
            $"CV EXTRACTION: {text.Length} characters extracted from DOCX.");

        return text;
    }
}