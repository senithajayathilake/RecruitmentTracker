using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace RecruitmentTracker.Services;

public class CvTextExtractor : ICvTextExtractor
{
    public Task<string> ExtractAsync(string filePath, string extension)
    {
        extension = extension.ToLowerInvariant();

        if (extension == ".pdf")
            return Task.FromResult(ExtractPdf(filePath));

        if (extension == ".docx")
            return Task.FromResult(ExtractDocx(filePath));

        return Task.FromResult(string.Empty);
    }

    private static string ExtractPdf(string path)
    {
        using var document = PdfDocument.Open(path);
        return string.Join(Environment.NewLine, document.GetPages().Select(p => p.Text));
    }

    private static string ExtractDocx(string path)
    {
        using var doc = WordprocessingDocument.Open(path, false);
        var body = doc.MainDocumentPart?.Document.Body;
        return body?.InnerText ?? string.Empty;
    }
}
