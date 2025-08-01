using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPortal.Models.DTOs;
using RecruitmentPortal.Models.ViewModels;
using RecruitmentPortal.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RecruitmentPortal.Controllers
{
    [Authorize]
    public class RfqController : Controller
    {
        private readonly IBusinessCentralRfqService _rfqService;
        private readonly BusinessCentralAuthService _authService;
        private readonly ILogger<RfqController> _logger;
        private readonly IColorSettingsService _colorSettingsService;

        public RfqController(
            IBusinessCentralRfqService rfqService,
            BusinessCentralAuthService authService,
            ILogger<RfqController> logger,
            IColorSettingsService colorSettingsService)
        {
            _rfqService = rfqService;
            _authService = authService;
            _logger = logger;
            _colorSettingsService = colorSettingsService;
        }


        [HttpGet]
        public async Task<IActionResult> Index(string status = null)
        {
            try
            {
                var colorSettings = await _colorSettingsService.GetColorSettingsAsync();
                var vendorNo = await _authService.GetVendorNoByEmailAsync(User.Identity.Name);
                if (string.IsNullOrEmpty(vendorNo))
                {
                    _logger.LogWarning("No vendor assigned to user {User}", User.Identity.Name);
                    return View(new RfqViewModel { ColorSettings = colorSettings });
                }

                var publishedLines = await _rfqService.GetPublishedRfqLinesAsync(vendorNo);

                // Get quoted line IDs
                var quotedLineIds = publishedLines
                    .Where(line => line.IsAlreadyQuoted)
                    .Select(line => line.SystemIdGuid)
                    .Where(guid => guid != Guid.Empty)
                    .ToHashSet();

                // Separate open and awarded lines
                var openLines = publishedLines
                    .Where(line => string.IsNullOrEmpty(line.AwardedToVendorNo)) // Only non-awarded
                    .ToList();

                var awardedLines = publishedLines
                    .Where(line => !string.IsNullOrEmpty(line.AwardedToVendorNo)) // Only awarded
                    .ToList();

                // Apply status filter only to open lines
                if (!string.IsNullOrEmpty(status))
                {
                    if (status.Equals("Awarded", StringComparison.OrdinalIgnoreCase))
                    {
                        // Show only awarded RFQs
                        openLines = new List<RfqPublishedLineDto>();
                    }
                    else
                    {
                        // Filter open lines by status
                        openLines = openLines.Where(line => line.RfqStatus == status).ToList();
                    }
                }

                var vendorCategories = await _rfqService.GetRfqVendorCategoriesAsync();

                var model = new RfqViewModel
                {
                    OpenLines = openLines,
                    AwardedLines = awardedLines,
                    VendorCategories = vendorCategories,
                    CurrentVendorNo = vendorNo,
                    QuotedLineIds = quotedLineIds,
                    CurrentFilter = status,
                    ColorSettings = colorSettings
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading RFQ index page");
                return View(new RfqViewModel
                {
                    ColorSettings = await _colorSettingsService.GetColorSettingsAsync()
                });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SubmitQuote([FromBody] QuoteSubmissionModel model)
        {
            try
            {
                var vendorNo = await _authService.GetVendorNoByEmailAsync(User.Identity.Name);
                if (string.IsNullOrEmpty(vendorNo))
                {
                    _logger.LogWarning("No vendor assigned to user {User}", User.Identity.Name);
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Vendor not assigned to user. Please contact your administrator."
                    });
                }

                if (string.IsNullOrEmpty(model.ItemNo))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Item number is required"
                    });
                }

                if (model.UnitCost <= 0)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Unit cost must be greater than zero"
                    });
                }

                if (model.VatOption == "Vatable" && model.VatPercentage <= 0)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "VAT percentage must be greater than zero for Vatable items"
                    });
                }

                if (model.Quantity <= 0)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Quantity must be greater than zero"
                    });
                }

                var result = await _rfqService.SubmitQuoteAsync(
                    vendorNo,
                    model.ItemNo,
                    model.UnitCost,
                    model.PriceIncludesVAT,
                    model.VatOption,
                    model.VatPercentage,
                    model.Quantity,
                    model.SystemId2);

                return Ok(new
                {
                    Success = true,
                    Message = "Quote submitted successfully",
                    DocumentNo = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting quote for item {ItemNo}", model?.ItemNo);
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAllQuotes([FromBody] List<QuoteSubmissionModel> models)
        {
            try
            {
                var vendorNo = await _authService.GetVendorNoByEmailAsync(User.Identity.Name);
                if (string.IsNullOrEmpty(vendorNo))
                {
                    _logger.LogWarning("No vendor assigned to user {User}", User.Identity.Name);
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Vendor not assigned to user. Please contact your administrator."
                    });
                }

                if (models == null || !models.Any())
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "No quotes were submitted."
                    });
                }

                // First check for basic required fields
                var invalidQuotes = models.Where(q =>
                    string.IsNullOrEmpty(q.ItemNo) ||
                    q.UnitCost <= 0 ||
                    q.Quantity <= 0).ToList();

                if (invalidQuotes.Any())
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = $"Invalid quotes detected. Please ensure all items have valid Item No, Unit Cost (> 0), and Quantity (> 0).",
                        InvalidItems = invalidQuotes.Select(q => q.ItemNo).ToList()
                    });
                }

                // Then check VAT-specific validation only for Vatable items
                var invalidVatQuotes = models
                    .Where(q => q.VatOption == "Vatable" && q.VatPercentage <= 0)
                    .ToList();

                if (invalidVatQuotes.Any())
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "VAT percentage must be greater than zero for Vatable items.",
                        InvalidVatItems = invalidVatQuotes.Select(q => q.ItemNo).ToList()
                    });
                }

                // All quotes are valid at this point
                string result;
                if (models.Count == 1)
                {
                    var quote = models.First();
                    result = await _rfqService.SubmitQuoteAsync(
                        vendorNo,
                        quote.ItemNo,
                        quote.UnitCost,
                        quote.PriceIncludesVAT,
                        quote.VatOption,
                        quote.VatPercentage,
                        quote.Quantity,
                        quote.SystemId2);
                }
                else
                {
                    result = await _rfqService.SubmitMultipleQuotesAsync(vendorNo, models);
                }

                return Ok(new
                {
                    Success = true,
                    Message = $"{models.Count} quotes submitted successfully",
                    DocumentNo = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting {Count} quotes", models?.Count);
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Open()
        {
            try
            {
                var vendorNo = await _authService.GetVendorNoByEmailAsync(User.Identity.Name);
                var defaultVatPercentage = await _rfqService.GetDefaultVatPercentageAsync();

                var colorSettings = await _colorSettingsService.GetColorSettingsAsync();

                if (string.IsNullOrEmpty(vendorNo))
                {
                    _logger.LogWarning("No vendor assigned to user {User}", User.Identity.Name);
                    return View(new RfqViewModel { ColorSettings = colorSettings });
                }

                var publishedLines = await _rfqService.GetPublishedRfqLinesAsync(vendorNo);

                // Get quoted line IDs
                var quotedLineIds = publishedLines
                    .Where(line => line.IsAlreadyQuoted)
                    .Select(line => line.SystemIdGuid)
                    .Where(guid => guid != Guid.Empty)
                    .ToHashSet();

                // Get open lines - non-awarded and with status "0" (Open)
                var openLines = publishedLines
                    .Where(line => string.IsNullOrEmpty(line.AwardedToVendorNo) &&
                                  (line.RfqStatus == "0" || line.RfqStatus?.ToLower() == "open"))
                    .ToList();

                var vendorCategories = await _rfqService.GetRfqVendorCategoriesAsync();

                return View("Index", new RfqViewModel
                {
                    OpenLines = openLines,
                    VendorCategories = vendorCategories,
                    CurrentVendorNo = "",
                    QuotedLineIds = quotedLineIds,
                    CurrentFilter = "0",
                    DefaultVatPercentage = defaultVatPercentage,
                    ColorSettings = colorSettings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading open RFQs");
                return View(new RfqViewModel
                {
                    ColorSettings = await _colorSettingsService.GetColorSettingsAsync()
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Awarded()
        {
            try
            {
                var vendorNo = await _authService.GetVendorNoByEmailAsync(User.Identity.Name);
                if (string.IsNullOrEmpty(vendorNo))
                {
                    _logger.LogWarning("No vendor assigned to user {User}", User.Identity.Name);
                    return View("Awarded", new RfqViewModel
                    {
                        ColorSettings = await _colorSettingsService.GetColorSettingsAsync()
                    });
                }

                var allLines = await _rfqService.GetPublishedRfqLinesAsync(vendorNo);
                var awardedLines = allLines
                    .Where(line => !string.IsNullOrEmpty(line.AwardedToVendorNo))
                    .Where(line => line.AwardedToVendorNo.Trim()
                            .Equals(vendorNo.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();

                _logger.LogInformation("Loaded {Count} awarded RFQ lines for vendor {VendorNo}",
                    awardedLines.Count, vendorNo);

                var model = new RfqViewModel
                {
                    AwardedLines = awardedLines,
                    CurrentVendorNo = vendorNo,
                    ColorSettings = await _colorSettingsService.GetColorSettingsAsync()
                };

                return View("Awarded", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading awarded RFQs");
                return View("Awarded", new RfqViewModel
                {
                    ColorSettings = await _colorSettingsService.GetColorSettingsAsync()
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Closed()
        {
            try
            {
                var vendorNo = await _authService.GetVendorNoByEmailAsync(User.Identity.Name);
                if (string.IsNullOrEmpty(vendorNo))
                {
                    _logger.LogWarning("No vendor assigned to user {User}", User.Identity.Name);
                    return View("Closed", new RfqViewModel
                    {
                        ColorSettings = await _colorSettingsService.GetColorSettingsAsync()
                    });
                }

                var closedLines = (await _rfqService.GetPublishedRfqLinesAsync(vendorNo))
                    .Where(line => line.RfqStatus == "1" && string.IsNullOrEmpty(line.AwardedToVendorNo))
                    .ToList();

                // Convert List<Guid> to HashSet<Guid>
                var quotedLineIdsList = await _rfqService.GetQuotedLineIdsAsync(vendorNo);
                var quotedLineIds = new HashSet<Guid>(quotedLineIdsList);

                var vendorCategories = await _rfqService.GetRfqVendorCategoriesAsync();

                var model = new RfqViewModel
                {
                    OpenLines = closedLines,
                    VendorCategories = vendorCategories,
                    CurrentVendorNo = vendorNo,
                    QuotedLineIds = quotedLineIds,
                    CurrentFilter = "1",
                    ColorSettings = await _colorSettingsService.GetColorSettingsAsync()
                };

                return View("Closed", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading closed RFQs");
                return View("Closed", new RfqViewModel
                {
                    ColorSettings = await _colorSettingsService.GetColorSettingsAsync()
                });
            }
        }
    }
}