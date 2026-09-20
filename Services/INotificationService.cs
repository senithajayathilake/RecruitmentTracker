namespace RecruitmentTracker.Services;

public interface INotificationService
{
    Task NotifyUserAsync(string? userId, string title, string message, string? linkUrl = null);
    Task NotifyRoleAsync(string role, string title, string message, string? linkUrl = null);
}
