using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPortal.Services;
using RecruitmentPortal.Models.ViewModels;
using System.Threading.Tasks;

namespace RecruitmentPortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBusinessCentralRfqService _rfqService;
        private readonly IBusinessCentralTenderService _tenderService;
        private readonly IBusinessCentralImprestItemService _requisitionService;
        private readonly BusinessCentralAuthService _authService;
        private readonly ILogger<HomeController> _logger;
        private readonly IColorSettingsService _colorSettingsService;

        public HomeController(
            IBusinessCentralRfqService rfqService,
            IBusinessCentralTenderService tenderService,
            IBusinessCentralImprestItemService requisitionService,
            BusinessCentralAuthService authService,
            ILogger<HomeController> logger,
            IColorSettingsService colorSettingsService)
        {
            _rfqService = rfqService;
            _tenderService = tenderService;
            _requisitionService = requisitionService;
            _authService = authService;
            _logger = logger;
            _colorSettingsService = colorSettingsService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var colorSettings = await _colorSettingsService.GetColorSettingsAsync();
                var vendorNo = await _authService.GetVendorNoByEmailAsync(User.Identity.Name);

                if (string.IsNullOrEmpty(vendorNo))
                {
                    return View(new DashboardViewModel
                    {
                        ColorSettings = colorSettings
                    });
                }

                // Get all RFQ lines
                var rfqLines = await _rfqService.GetPublishedRfqLinesAsync(vendorNo);

                // Count open RFQs (status = 0 and not awarded)
                int openRfqCount = rfqLines.Count(l => l.RfqStatus == "0" && string.IsNullOrEmpty(l.AwardedToVendorNo));

                // Count closed RFQs (status = 1 and not awarded)
                int closedRfqCount = rfqLines.Count(l => l.RfqStatus == "1" && string.IsNullOrEmpty(l.AwardedToVendorNo));

                // Count awarded RFQs (must have AwardedToVendorNo AND match current vendor)
                int awardedRfqCount = rfqLines.Count(l =>
                    !string.IsNullOrEmpty(l.AwardedToVendorNo) &&
                    l.AwardedToVendorNo.Trim().Equals(vendorNo.Trim(), StringComparison.OrdinalIgnoreCase));

                // Get Tender counts
                var (openTenderCount, closedTenderCount, awardedTenderCount) =
                    await _tenderService.GetTenderCountsByStatusAsync(vendorNo);

                // Get Requisition counts
                var allRequisitions = await _requisitionService.GetRequisitionHeadersAsync(User.Identity.Name);

                return View(new DashboardViewModel
                {
                    OpenRfqCount = openRfqCount,
                    ClosedRfqCount = closedRfqCount,
                    AwardedRfqCount = awardedRfqCount,
                    OpenTenderCount = openTenderCount,
                    ClosedTenderCount = closedTenderCount,
                    AwardedTenderCount = awardedTenderCount,
                    OpenImprestRequisitionCount = allRequisitions.Count(r =>
                        (r.Status == "0" || r.Status.Equals("Open", StringComparison.OrdinalIgnoreCase)) &&
                        r.RequisitionType == "Imprest"),
                    CancelledItemRequisitionCount = allRequisitions.Count(r =>
                        (r.Status == "2" || r.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)) &&
                        r.RequisitionType == "Item"),
                    CompletedRequisitionCount = allRequisitions.Count(r =>
                        r.Status == "1" || r.Status.Equals("Posted", StringComparison.OrdinalIgnoreCase)),
                    ApprovedRequisitionCount = allRequisitions.Count(r =>
                        r.Status == "3" || r.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase)),
                    ColorSettings = colorSettings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                return View(new DashboardViewModel
                {
                    ColorSettings = await _colorSettingsService.GetColorSettingsAsync()
                });
            }
        }
    }
}