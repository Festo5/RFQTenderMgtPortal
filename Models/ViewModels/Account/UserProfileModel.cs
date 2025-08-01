using System.ComponentModel.DataAnnotations;

public class UserProfileModel
{
    public string Email { get; set; }

    [Display(Name = "Company Name")]
    [MaxLength(100)]
    public string CompanyName { get; set; }

    [Display(Name = "Phone Number")]
    [Phone]
    [MaxLength(30)]
    public string PhoneNumber { get; set; }

    [Display(Name = "Profile Picture")]
    public IFormFile ProfilePicture { get; set; }

    public ColorSettingsViewModel ColorSettings { get; set; }
}