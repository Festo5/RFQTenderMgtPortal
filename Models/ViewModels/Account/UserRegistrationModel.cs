using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models.ViewModels.Account;

public class UserRegistrationModel
{
    [Required]
    public string FullName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords don't match.")]
    public string ConfirmPassword { get; set; }
    public ColorSettingsViewModel ColorSettings { get; set; }
}