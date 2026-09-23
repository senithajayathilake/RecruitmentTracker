using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models;

public class OfferLetter
{
    public int ApplicationId { get; set; }

    public string OfferReference { get; set; } = string.Empty;

    public string CandidateName { get; set; } = string.Empty;

    public string CandidateEmail { get; set; } = string.Empty;

    public string JobTitle { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string EmploymentType { get; set; } = string.Empty;


    [Required]
    [Range(1, 999999999)]
    [Display(Name = "Salary")]
    public decimal Salary { get; set; }


    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "LKR";


    [Required]
    [StringLength(30)]
    [Display(Name = "Salary period")]
    public string SalaryPeriod { get; set; } = "per month";


    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Start date")]
    public DateTime StartDate { get; set; }


    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Offer valid until")]
    public DateTime OfferExpiryDate { get; set; }


    [Required]
    [StringLength(150)]
    [Display(Name = "Work location")]
    public string WorkLocation { get; set; } = string.Empty;


    [StringLength(2000)]
    [Display(Name = "Additional notes")]
    public string? Notes { get; set; }


    public DateTime IssuedDate { get; set; }

    public DateTime UpdatedDate { get; set; }

    public string IssuedByName { get; set; } = string.Empty;
}