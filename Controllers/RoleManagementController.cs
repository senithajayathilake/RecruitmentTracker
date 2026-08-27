using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers;

[Authorize(Roles = "HR")]
public class RoleManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _db;

    private static readonly string[] AllowedRoles =
        ["Candidate", "HR", "HiringManager", "Interviewer"];

    public RoleManagementController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.Email)
            .ToListAsync();

        var rows = new List<UserRoleRowViewModel>();
        foreach (var user in users)
        {
            rows.Add(new UserRoleRowViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? user.UserName ?? "",
                Roles = (await _userManager.GetRolesAsync(user)).ToList()
            });
        }

        ViewBag.Roles = AllowedRoles;
        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(RoleAssignmentViewModel model)
    {
        ViewBag.Roles = AllowedRoles;

        if (!AllowedRoles.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Invalid role.");
            return await IndexWithModelErrors();
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(nameof(model.Email),
                "No account exists for this email. Ask the person to register first.");
            return await IndexWithModelErrors();
        }

        if (!await _roleManager.RoleExistsAsync(model.Role))
            await _roleManager.CreateAsync(new IdentityRole(model.Role));

        // Keep one primary portal role per account so login always has an
        // unambiguous destination.
        var existingRoles = await _userManager.GetRolesAsync(user);
        var otherPortalRoles = existingRoles.Where(r => AllowedRoles.Contains(r) && r != model.Role).ToList();
        if (otherPortalRoles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, otherPortalRoles);

        if (!await _userManager.IsInRoleAsync(user, model.Role))
        {
            var result = await _userManager.AddToRoleAsync(user, model.Role);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return await IndexWithModelErrors();
            }
        }

        if (model.Role == "Candidate")
        {
            var profile = await _db.Candidates.FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);
            if (profile == null)
            {
                _db.Candidates.Add(new Candidate
                {
                    ApplicationUserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? model.Email,
                    Status = "Added"
                });
                await _db.SaveChangesAsync();
            }
        }

        TempData["Success"] = $"{model.Email} now has the {model.Role} role.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(string userId, string role)
    {
        if (!AllowedRoles.Contains(role))
            return BadRequest();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (role == "HR" && user.Email?.Equals(User.Identity?.Name, StringComparison.OrdinalIgnoreCase) == true)
        {
            TempData["Error"] = "You cannot remove your own HR access.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.RemoveFromRoleAsync(user, role);
        TempData["Success"] = $"{role} access removed from {user.Email}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> IndexWithModelErrors()
    {
        ViewBag.Roles = AllowedRoles;
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        var rows = new List<UserRoleRowViewModel>();

        foreach (var user in users)
        {
            rows.Add(new UserRoleRowViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? user.UserName ?? "",
                Roles = (await _userManager.GetRolesAsync(user)).ToList()
            });
        }

        return View("Index", rows);
    }
}
