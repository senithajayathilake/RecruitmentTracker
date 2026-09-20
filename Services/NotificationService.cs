using Microsoft.AspNetCore.Identity;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task NotifyUserAsync(
        string? userId,
        string title,
        string message,
        string? linkUrl = null)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            LinkUrl = linkUrl,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public async Task NotifyRoleAsync(
        string role,
        string title,
        string message,
        string? linkUrl = null)
    {
        var users = await _userManager.GetUsersInRoleAsync(role);
        if (users.Count == 0) return;

        foreach (var user in users)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = title,
                Message = message,
                LinkUrl = linkUrl,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
    }
}
