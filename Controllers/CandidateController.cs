using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers;

[Authorize]
public class CandidateController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public CandidateController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment)
    {
        _db = db;
        _userManager = userManager;
        _environment = environment;
    }

    // =========================================================
    // HR - CANDIDATE LIST
    // =========================================================

    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Index()
    {
        var candidates = await _db.Candidates
            .Include(c => c.Cvs)
            .Include(c => c.Applications)
                .ThenInclude(a => a.Vacancy)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync();

        return View(candidates);
    }

    // =========================================================
    // HR - CREATE CANDIDATE
    // =========================================================

    [Authorize(Roles = "HR")]
    public IActionResult Create()
    {
        return View(new Candidate());
    }

    [Authorize(Roles = "HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Candidate model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _db.Candidates.Add(model);

        await _db.SaveChangesAsync();

        TempData["Success"] =
            "Candidate added successfully.";

        return RedirectToAction(nameof(Index));
    }

    // =========================================================
    // HR - CANDIDATE DETAILS
    // =========================================================

    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Details(int id)
    {
        var candidate = await _db.Candidates
            .Include(c => c.Cvs
                .OrderByDescending(x => x.UploadedDate))
            .Include(c => c.Applications)
                .ThenInclude(a => a.Vacancy)
            .FirstOrDefaultAsync(
                c => c.CandidateId == id);

        if (candidate == null)
        {
            return NotFound();
        }

        return View(candidate);
    }

    // =========================================================
    // CANDIDATE - PROFILE
    // =========================================================

    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Profile()
    {
        var user =
            await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }

        var candidate =
            await _db.Candidates
                .FirstOrDefaultAsync(
                    c => c.ApplicationUserId == user.Id);

        if (candidate == null)
        {
            return NotFound();
        }

        return View(candidate);
    }

    // =========================================================
    // CANDIDATE - UPDATE PROFILE + AVATAR
    // =========================================================

    [Authorize(Roles = "Candidate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(
        Candidate model,
        IFormFile? avatar)
    {
        var user =
            await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }

        var candidate =
            await _db.Candidates
                .FirstOrDefaultAsync(
                    c => c.ApplicationUserId == user.Id);

        if (candidate == null)
        {
            return NotFound();
        }

        /*
         * Email comes from the logged-in Identity account.
         * The email field is disabled in the profile form.
         */
        ModelState.Remove(nameof(Candidate.Email));

        // =====================================================
        // AVATAR VALIDATION
        // =====================================================

        if (avatar != null &&
            avatar.Length > 0)
        {
            const long maximumFileSize =
                2 * 1024 * 1024;

            var extension =
                Path.GetExtension(avatar.FileName)
                    .ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png"
            };

            var allowedContentTypes = new[]
            {
                "image/jpeg",
                "image/png"
            };

            if (avatar.Length > maximumFileSize)
            {
                ModelState.AddModelError(
                    "avatar",
                    "Profile image must be 2 MB or smaller.");
            }

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "avatar",
                    "Only JPG, JPEG or PNG images are allowed.");
            }

            if (string.IsNullOrWhiteSpace(avatar.ContentType) ||
                !allowedContentTypes.Contains(
                    avatar.ContentType.ToLowerInvariant()))
            {
                ModelState.AddModelError(
                    "avatar",
                    "Please select a valid JPG or PNG image.");
            }
        }

        // =====================================================
        // VALIDATION FAILED
        // =====================================================

        if (!ModelState.IsValid)
        {
            model.CandidateId =
                candidate.CandidateId;

            model.Email =
                user.Email ??
                candidate.Email;

            return View(model);
        }

        // =====================================================
        // UPDATE PROFILE
        // =====================================================

        candidate.FullName =
            model.FullName;

        candidate.Email =
            user.Email ??
            candidate.Email;

        candidate.Phone =
            model.Phone;

        candidate.Address =
            model.Address;

        candidate.DateOfBirth =
            model.DateOfBirth;

        candidate.HighestQualification =
            model.HighestQualification;

        candidate.YearsOfExperience =
            model.YearsOfExperience;

        candidate.Skills =
            model.Skills;

        // =====================================================
        // SAVE AVATAR
        // =====================================================

        if (avatar != null &&
            avatar.Length > 0)
        {
            var avatarFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "avatars");

            Directory.CreateDirectory(
                avatarFolder);

            var extension =
                Path.GetExtension(avatar.FileName)
                    .ToLowerInvariant();

            /*
             * IMPORTANT:
             * Every upload gets a new filename.
             *
             * We DO NOT delete or overwrite the previous
             * profile image because Windows/browser may still
             * have the old image open.
             */

            var fileName =
                $"candidate-{candidate.CandidateId}-" +
                $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-" +
                $"{Guid.NewGuid():N}" +
                $"{extension}";

            var filePath =
                Path.Combine(
                    avatarFolder,
                    fileName);

            await using var stream =
                new FileStream(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

            await avatar.CopyToAsync(stream);
        }

        // Keep Identity display name updated.
        user.FullName =
            candidate.FullName;

        await _userManager.UpdateAsync(user);

        await _db.SaveChangesAsync();

        TempData["Success"] =
            "Profile updated successfully.";

        return RedirectToAction(
            nameof(Profile));
    }

    // =========================================================
    // RETURN THE LATEST AVATAR
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Avatar(int id)
    {
        var candidate =
            await _db.Candidates
                .FirstOrDefaultAsync(
                    c => c.CandidateId == id);

        if (candidate == null)
        {
            return NotFound();
        }

        /*
         * Candidate users can only view their own image.
         * HR and Hiring Manager can view candidate images.
         */
        if (User.IsInRole("Candidate"))
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null ||
                candidate.ApplicationUserId != user.Id)
            {
                return Forbid();
            }
        }
        else if (!User.IsInRole("HR") &&
                 !User.IsInRole("HiringManager"))
        {
            return Forbid();
        }

        var avatarFolder =
            Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "avatars");

        if (!Directory.Exists(avatarFolder))
        {
            return NotFound();
        }

        var allowedExtensions = new[]
        {
            ".jpg",
            ".jpeg",
            ".png"
        };

        // Find newest avatar created by the new system.
        var avatarFile =
            Directory
                .EnumerateFiles(
                    avatarFolder,
                    $"candidate-{id}-*.*")
                .Where(file =>
                    allowedExtensions.Contains(
                        Path.GetExtension(file)
                            .ToLowerInvariant()))
                .OrderByDescending(
                    file =>
                        System.IO.File.GetLastWriteTimeUtc(file))
                .FirstOrDefault();

        /*
         * Compatibility with profile pictures created
         * using the older candidate-3.jpg naming system.
         */
        if (avatarFile == null)
        {
            var oldFileNames = new[]
            {
                $"candidate-{id}.jpg",
                $"candidate-{id}.jpeg",
                $"candidate-{id}.png"
            };

            foreach (var oldFileName in oldFileNames)
            {
                var oldPath =
                    Path.Combine(
                        avatarFolder,
                        oldFileName);

                if (System.IO.File.Exists(oldPath))
                {
                    avatarFile = oldPath;
                    break;
                }
            }
        }

        if (avatarFile == null)
        {
            return NotFound();
        }

        var extension =
            Path.GetExtension(avatarFile)
                .ToLowerInvariant();

        var contentType =
            extension == ".png"
                ? "image/png"
                : "image/jpeg";

        // Always show the latest image.
        Response.Headers.CacheControl =
            "no-store, no-cache, must-revalidate";

        Response.Headers.Pragma =
            "no-cache";

        Response.Headers.Expires =
            "0";

        return PhysicalFile(
            avatarFile,
            contentType);
    }

    // =========================================================
    // HR - UPDATE CANDIDATE STATUS
    // =========================================================

    [Authorize(Roles = "HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(
        int id,
        string status)
    {
        var candidate =
            await _db.Candidates.FindAsync(id);

        if (candidate == null)
        {
            return NotFound();
        }

        candidate.Status =
            status;

        await _db.SaveChangesAsync();

        return RedirectToAction(
            nameof(Details),
            new { id });
    }
}