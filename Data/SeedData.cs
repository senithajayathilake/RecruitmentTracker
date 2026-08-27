using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        string[] roles = ["HR", "Candidate", "Interviewer", "HiringManager"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await CreateUser(userManager, "HR Officer", "hr@recruitment.local", "Hr12345", "HR");
        var candidate = await CreateUser(userManager, "Demo Candidate", "candidate@recruitment.local", "Candidate123", "Candidate");
        await CreateUser(userManager, "Demo Interviewer", "interviewer@recruitment.local", "Interviewer123", "Interviewer");
        await CreateUser(userManager, "Hiring Manager", "manager@recruitment.local", "Manager123", "HiringManager");

        if (candidate != null)
        {
            var profile = await db.Candidates.FirstOrDefaultAsync(c => c.ApplicationUserId == candidate.Id);
            if (profile == null)
            {
                db.Candidates.Add(new Candidate
                {
                    ApplicationUserId = candidate.Id,
                    FullName = candidate.FullName,
                    Email = candidate.Email ?? "",
                    Status = "Added"
                });
                await db.SaveChangesAsync();
            }
        }
    }

    private static async Task<ApplicationUser?> CreateUser(
        UserManager<ApplicationUser> userManager,
        string fullName,
        string email,
        string password,
        string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            if (!await userManager.IsInRoleAsync(existing, role))
                await userManager.AddToRoleAsync(existing, role);
            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
            return user;
        }

        return null;
    }
}
