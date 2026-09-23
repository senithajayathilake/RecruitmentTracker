using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;
using RecruitmentTracker.Services;

namespace RecruitmentTracker.Controllers;

[Authorize]
public class OfferLetterController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly INotificationService _notifications;

    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            WriteIndented = true
        };

    public OfferLetterController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        INotificationService notifications)
    {
        _db = db;
        _userManager = userManager;
        _environment = environment;
        _notifications = notifications;
    }

    // =========================================================
    // STORAGE
    // =========================================================

    private string OfferFolder =>
        Environment.GetEnvironmentVariable("OFFER_LETTER_DIRECTORY") is { Length: > 0 } folder
            ? Path.GetFullPath(folder)
            : Path.Combine(_environment.ContentRootPath, "App_Data", "OfferLetters");

    private string GetOfferPath(int applicationId)
    {
        return Path.Combine(
            OfferFolder,
            $"offer-{applicationId}.json");
    }

    private bool OfferExists(int applicationId)
    {
        return System.IO.File.Exists(
            GetOfferPath(applicationId));
    }

    private async Task<OfferLetter?> LoadOfferAsync(
        int applicationId)
    {
        var path =
            GetOfferPath(applicationId);

        if (!System.IO.File.Exists(path))
            return null;

        try
        {
            var json =
                await System.IO.File.ReadAllTextAsync(path);

            return JsonSerializer.Deserialize<OfferLetter>(
                json,
                _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveOfferAsync(
        OfferLetter offer)
    {
        Directory.CreateDirectory(
            OfferFolder);

        var path =
            GetOfferPath(offer.ApplicationId);

        var json =
            JsonSerializer.Serialize(
                offer,
                _jsonOptions);

        var temporaryPath =
            path + "." +
            Guid.NewGuid().ToString("N") +
            ".tmp";

        await System.IO.File.WriteAllTextAsync(
            temporaryPath,
            json);

        System.IO.File.Move(
            temporaryPath,
            path,
            true);
    }

    // =========================================================
    // HR - OFFER LETTER LIST
    // =========================================================

    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Index()
    {
        var applications =
            await _db.Applications
                .Where(a => a.Status == "Hired")
                .Include(a => a.Candidate)
                .Include(a => a.Vacancy)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

        var offerIds =
            applications
                .Where(a => OfferExists(a.ApplicationId))
                .Select(a => a.ApplicationId)
                .ToHashSet();

        ViewBag.OfferIds =
            offerIds;

        return View(applications);
    }

    // =========================================================
    // HR - CREATE / EDIT OFFER
    // =========================================================

    [Authorize(Roles = "HR")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var application =
            await _db.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Vacancy)
                .FirstOrDefaultAsync(
                    a => a.ApplicationId == id);

        if (application == null)
            return NotFound();

        if (application.Status != "Hired")
        {
            TempData["Error"] =
                "An offer letter can only be created after the candidate is marked as Hired.";

            return RedirectToAction(
                nameof(Index));
        }

        var existing =
            await LoadOfferAsync(id);

        var offer =
            existing ??
            new OfferLetter
            {
                ApplicationId =
                    application.ApplicationId,

                Currency =
                    "LKR",

                SalaryPeriod =
                    "per month",

                StartDate =
                    DateTime.Today.AddDays(14),

                OfferExpiryDate =
                    DateTime.Today.AddDays(7),

                WorkLocation =
                    application.Vacancy?.Location
                    ?? string.Empty
            };

        PopulateApplicationInformation(
            offer,
            application);

        return View(offer);
    }

    // =========================================================
    // HR - SAVE / ISSUE OFFER
    // =========================================================

    [Authorize(Roles = "HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        OfferLetter model)
    {
        var application =
            await _db.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Vacancy)
                .FirstOrDefaultAsync(
                    a => a.ApplicationId == id);

        if (application == null)
            return NotFound();

        if (application.Status != "Hired")
        {
            TempData["Error"] =
                "An offer letter can only be created for a hired candidate.";

            return RedirectToAction(
                nameof(Index));
        }

        model.ApplicationId =
            application.ApplicationId;

        PopulateApplicationInformation(
            model,
            application);

        if (model.OfferExpiryDate.Date >
            model.StartDate.Date)
        {
            ModelState.AddModelError(
                nameof(model.OfferExpiryDate),
                "The offer expiry date must be on or before the start date.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing =
            await LoadOfferAsync(id);

        var currentUser =
            await _userManager.GetUserAsync(User);

        model.OfferReference =
            existing?.OfferReference
            ?? $"HF-{DateTime.UtcNow:yyyy}-{id:0000}";

        model.IssuedDate =
            existing?.IssuedDate
            ?? DateTime.UtcNow;

        model.UpdatedDate =
            DateTime.UtcNow;

        model.IssuedByName =
            currentUser?.FullName
            ?? currentUser?.Email
            ?? "Higher Flow Recruitment";

        await SaveOfferAsync(model);

        if (application.Candidate != null &&
            !string.IsNullOrWhiteSpace(
                application.Candidate.ApplicationUserId))
        {
            await _notifications.NotifyUserAsync(
                application.Candidate.ApplicationUserId,

                existing == null
                    ? "Offer letter issued"
                    : "Offer letter updated",

                $"Your offer letter for {application.Vacancy?.JobTitle} is available.",

                $"/OfferLetter/Details/{application.ApplicationId}");
        }

        TempData["Success"] =
            existing == null
                ? "Offer letter issued successfully."
                : "Offer letter updated successfully.";

        return RedirectToAction(
            nameof(Details),
            new
            {
                id = application.ApplicationId
            });
    }

    // =========================================================
    // CANDIDATE - MY OFFERS
    // =========================================================

    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> MyOffers()
    {
        var user =
            await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var candidate =
            await _db.Candidates
                .FirstOrDefaultAsync(
                    c => c.ApplicationUserId == user.Id);

        if (candidate == null)
            return NotFound();

        var applications =
            await _db.Applications
                .Where(a =>
                    a.CandidateId == candidate.CandidateId &&
                    a.Status == "Hired")
                .Include(a => a.Vacancy)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

        var issuedOffers =
            applications
                .Where(a =>
                    OfferExists(a.ApplicationId))
                .ToList();

        return View(issuedOffers);
    }

    // =========================================================
    // VIEW OFFER
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var application =
            await _db.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Vacancy)
                .FirstOrDefaultAsync(
                    a => a.ApplicationId == id);

        if (application == null)
            return NotFound();

        if (User.IsInRole("Candidate"))
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null ||
                application.Candidate?
                    .ApplicationUserId != user.Id)
            {
                return Forbid();
            }
        }
        else if (!User.IsInRole("HR") &&
                 !User.IsInRole("HiringManager"))
        {
            return Forbid();
        }

        var offer =
            await LoadOfferAsync(id);

        if (offer == null)
        {
            TempData["Error"] =
                "An offer letter has not been issued yet.";

            if (User.IsInRole("Candidate"))
            {
                return RedirectToAction(
                    nameof(MyOffers));
            }

            if (User.IsInRole("HR"))
            {
                return RedirectToAction(
                    nameof(Index));
            }

            return RedirectToAction(
                "Details",
                "Application",
                new
                {
                    id
                });
        }

        return View(offer);
    }

    // =========================================================
    // HELPER
    // =========================================================

    private static void PopulateApplicationInformation(
        OfferLetter offer,
        Application application)
    {
        offer.ApplicationId =
            application.ApplicationId;

        offer.CandidateName =
            application.Candidate?.FullName
            ?? "Candidate";

        offer.CandidateEmail =
            application.Candidate?.Email
            ?? string.Empty;

        offer.JobTitle =
            application.Vacancy?.JobTitle
            ?? string.Empty;

        offer.Department =
            application.Vacancy?.Department
            ?? string.Empty;

        offer.EmploymentType =
            application.Vacancy?.EmploymentType
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(
            offer.WorkLocation))
        {
            offer.WorkLocation =
                application.Vacancy?.Location
                ?? string.Empty;
        }
    }
}