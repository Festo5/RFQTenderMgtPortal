using RecruitmentPortal.ValidationAttributes;
using System.ComponentModel.DataAnnotations;

public class JobApplicationModel
{
    [Required]
    public int JobId { get; set; }

    [Required]
    public int CandidateId { get; set; }

    public string JobTitle { get; set; } // Add this for display purposes

    [Required]
    [Display(Name = "Cover Letter")]
    [StringLength(5000, ErrorMessage = "Cover letter cannot exceed 5000 characters.")]
    public string CoverLetter { get; set; }

    [Required]
    [Display(Name = "Resume")]
    [DataType(DataType.Upload)]
    [AllowedExtensions(new string[] { ".pdf", ".doc", ".docx" })]
    [MaxFileSize(5 * 1024 * 1024)] // 5MB
    public IFormFile ResumeFile { get; set; }

    public string? ResumePath { get; set; }
}