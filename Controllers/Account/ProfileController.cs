using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPortal.Models.ViewModels.Account;
using RecruitmentPortal.Services;
using System.Security.Claims;

[Authorize]
public class ProfileController : Controller
{
    private readonly BusinessCentralAuthService _bcAuthService;
    private readonly IColorSettingsService _colorSettingsService;

    public ProfileController(
        BusinessCentralAuthService bcAuthService,
        IColorSettingsService colorSettingsService)
    {
        _bcAuthService = bcAuthService;
        _colorSettingsService = colorSettingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var profile = await _bcAuthService.GetUserProfileAsync(email);

        var model = new UserProfileModel
        {
            Email = email,
            CompanyName = profile?.CompanyName,
            PhoneNumber = profile?.PhoneNumber,
            ColorSettings = await _colorSettingsService.GetColorSettingsAsync() // Ensure this is set
        };

        if (profile?.ProfilePicture != null)
        {
            ViewBag.ProfilePictureBase64 = Convert.ToBase64String(profile.ProfilePicture);
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Index(UserProfileModel model)
    {
        // Get color settings first before validation
        model.ColorSettings = await _colorSettingsService.GetColorSettingsAsync();

        // Explicitly remove ColorSettings from validation
        ModelState.Remove("ColorSettings");

        if (!ModelState.IsValid)
        {
            // Reload profile picture if validation fails
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            var currentProfile = await _bcAuthService.GetUserProfileAsync(currentEmail);
            if (currentProfile?.ProfilePicture != null)
            {
                ViewBag.ProfilePictureBase64 = Convert.ToBase64String(currentProfile.ProfilePicture);
            }

            return View(model);
        }

        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        byte[] profileBytes = null;

        if (model.ProfilePicture != null)
        {
            using var memoryStream = new MemoryStream();
            await model.ProfilePicture.CopyToAsync(memoryStream);

            // Validate file size (max 5MB)
            if (memoryStream.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ProfilePicture", "The profile picture must be less than 5MB.");
                return View(model);
            }

            profileBytes = memoryStream.ToArray();
        }

        var success = await _bcAuthService.UpdateUserProfileAsync(
            userEmail,
            model.CompanyName,
            model.PhoneNumber,
            profileBytes);

        if (!success)
        {
            ModelState.AddModelError("", "Failed to update profile. Please try again.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Profile updated successfully!";
        return RedirectToAction(nameof(Index));
    }
}