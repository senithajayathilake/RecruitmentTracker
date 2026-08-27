using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;
using RecruitmentTracker.Services;

namespace RecruitmentTracker.Controllers;

[Authorize(Roles = "HR,Candidate")]
public class CVController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly ICvTextExtractor _extractor;
    private readonly UserManager<ApplicationUser> _userManager;

    private static readonly string[] AllowedExtensions = [".pdf", ".docx"];

    public CVController(
        ApplicationDbContext db,
        IWebHostEnvironment environment,
        ICvTextExtractor extractor,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _environment = environment;
        _extractor = extractor;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Upload(int? candidateId = null)
    {
        if (User.IsInRole("Candidate"))
        {
            var user = await _userManager.GetUserAsync(User);
            var ownCandidate = await _db.Candidates.FirstOrDefaultAsync(c => c.ApplicationUserId == user!.Id);
            if (ownCandidate == null) return NotFound();
            candidateId = ownCandidate.CandidateId;
        }

        if (!candidateId.HasValue) return BadRequest("Candidate ID is required for HR uploads.");

        var candidate = await _db.Candidates.FindAsync(candidateId.Value);
        if (candidate == null) return NotFound();

        return View(new CvUploadViewModel { CandidateId = candidate.CandidateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(CvUploadViewModel model)
    {
        var candidate = await _db.Candidates.FindAsync(model.CandidateId);
        if (candidate == null) return NotFound();

        if (User.IsInRole("Candidate"))
        {
            var user = await _userManager.GetUserAsync(User);
            if (candidate.ApplicationUserId != user!.Id)
                return Forbid();
        }

        if (model.File == null || model.File.Length == 0)
            ModelState.AddModelError(nameof(model.File), "Please select a CV.");

        var extension = Path.GetExtension(model.File?.FileName ?? "").ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            ModelState.AddModelError(nameof(model.File), "Only PDF and DOCX files are allowed.");

        if (model.File?.Length > 10 * 1024 * 1024)
            ModelState.AddModelError(nameof(model.File), "Maximum file size is 10 MB.");

        if (!ModelState.IsValid) return View(model);

        var folder = Path.Combine(_environment.WebRootPath, "uploads", "cvs");
        Directory.CreateDirectory(folder);

        var safeName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, safeName);

        await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
        {
            await model.File!.CopyToAsync(stream);
        }

        var relativePath = $"/uploads/cvs/{safeName}";
        var extractedText = await _extractor.ExtractAsync(fullPath, extension);

        _db.CandidateCvs.Add(new CandidateCv
        {
            CandidateId = candidate.CandidateId,
            FileName = Path.GetFileName(model.File.FileName),
            FilePath = relativePath,
            ExtractedText = extractedText
        });

        candidate.Status = "Screening";
        await _db.SaveChangesAsync();

        TempData["Success"] = "CV uploaded successfully. HR can now see your profile and screen this CV.";
        return User.IsInRole("Candidate")
            ? RedirectToAction("Candidate", "Dashboard")
            : RedirectToAction("Details", "Candidate", new { id = candidate.CandidateId });
    }
}
