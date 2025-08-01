// ViewComponents/GlobalSettingsViewComponent.cs
using Microsoft.AspNetCore.Mvc;
using RecruitmentPortal.Services;

public class GlobalSettingsViewComponent : ViewComponent
{
    private readonly IBusinessCentralRfqService _rfqService;
    private readonly BusinessCentralAuthService _authService;

    public GlobalSettingsViewComponent(
        IBusinessCentralRfqService rfqService,
        BusinessCentralAuthService authService)
    {
        _rfqService = rfqService;
        _authService = authService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new GlobalSettingsViewModel
        {
            DefaultVatPercentage = await _rfqService.GetDefaultVatPercentageAsync(),
            VendorNo = await _authService.GetVendorNoByEmailAsync(User.Identity.Name)
        };
        return View(model);
    }
}

public class GlobalSettingsViewModel
{
    public decimal DefaultVatPercentage { get; set; }
    public string VendorNo { get; set; }
}