using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models;

public class Notification
{
    public int NotificationId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(800)]
    public string Message { get; set; } = string.Empty;

    [StringLength(300)]
    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
