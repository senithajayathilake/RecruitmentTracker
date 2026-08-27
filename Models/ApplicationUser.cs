using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models;

public class ApplicationUser : IdentityUser
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;
}
