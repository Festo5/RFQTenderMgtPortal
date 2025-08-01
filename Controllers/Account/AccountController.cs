using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using RecruitmentPortal.Models.ViewModels.Account;
using RecruitmentPortal.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RecruitmentPortal.Controllers.Account;

public class AccountController : Controller
{
    private readonly BusinessCentralAuthService _bcAuthService;
    private readonly PasswordHasher _passwordHasher;
    private readonly IColorSettingsService _colorSettingsService;

    public AccountController(
        BusinessCentralAuthService bcAuthService,
        PasswordHasher passwordHasher,
        IColorSettingsService colorSettingsService)
    {
        _bcAuthService = bcAuthService;
        _passwordHasher = passwordHasher;
        _colorSettingsService = colorSettingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        var colorSettings = await _colorSettingsService.GetColorSettingsAsync();
        return View(new UserRegistrationModel { ColorSettings = colorSettings });
    }

    [HttpPost]
    public async Task<IActionResult> Register(UserRegistrationModel model)
    {
        if (!ModelState.IsValid)
        {
            model.ColorSettings = await _colorSettingsService.GetColorSettingsAsync();
            return View(model);
        }

        var hashedPassword = _passwordHasher.HashPassword(model.Password);
        var success = await _bcAuthService.CreateUserAsync(model.Email, hashedPassword, model.FullName);

        if (!success)
        {
            ModelState.AddModelError("", "Registration failed. User may already exist or not active.");
            model.ColorSettings = await _colorSettingsService.GetColorSettingsAsync();
            return View(model);
        }

        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        var colorSettings = await _colorSettingsService.GetColorSettingsAsync();
        return View(new LoginModel { ColorSettings = colorSettings });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginModel model)
    {
        // Get color settings first
        var colorSettings = await _colorSettingsService.GetColorSettingsAsync();
        model.ColorSettings = colorSettings;

        // Explicitly remove ColorSettings from validation
        ModelState.Remove("ColorSettings");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var hashedPassword = _passwordHasher.HashPassword(model.Password);
        var isValid = await _bcAuthService.ValidateUserAsync(model.Email, hashedPassword);

        if (!isValid)
        {
            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        // Get candidate ID from Business Central
        int candidateId = await _bcAuthService.GetCandidateIdAsync(model.Email);

        if (candidateId <= 0)
        {
            ModelState.AddModelError("", "Could not retrieve candidate information.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, model.Email),
            new Claim(ClaimTypes.Email, model.Email),
            new Claim("CandidateId", candidateId.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = model.RememberMe });

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}