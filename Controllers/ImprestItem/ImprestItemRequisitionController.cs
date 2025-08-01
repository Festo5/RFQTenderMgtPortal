using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPortal.Models.DTOs;
using RecruitmentPortal.Models.ViewModels;
using RecruitmentPortal.Models.ViewModels.ImprestItem;
using RecruitmentPortal.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RecruitmentPortal.Controllers
{
    [Authorize]
    public class ImprestItemRequisitionController : Controller
    {
        private readonly IConfiguration _config;

        private readonly IBusinessCentralImprestItemService _requisitionService;
        private readonly BusinessCentralAuthService _authService;
        private readonly ILogger<ImprestItemRequisitionController> _logger;
        private readonly IColorSettingsService _colorSettingsService;

        private readonly IHttpClientFactory _httpClientFactory;

        public ImprestItemRequisitionController(
            IBusinessCentralImprestItemService requisitionService,
            BusinessCentralAuthService authService,
            ILogger<ImprestItemRequisitionController> logger,
            IColorSettingsService colorSettingsService,
            IConfiguration config,
            IHttpClientFactory httpClientFactory)
        {
            _requisitionService = requisitionService;
            _authService = authService;
            _logger = logger;
            _colorSettingsService = colorSettingsService;
            _config = config;
            _httpClientFactory = httpClientFactory; // Store the factory
        }

        [HttpGet]
        public async Task<IActionResult> Index(string status = null)
        {
            try
            {
                var userId = User.Identity.Name;
                var allHeaders = await _requisitionService.GetRequisitionHeadersAsync(userId);

                var colorSettings = await _colorSettingsService.GetColorSettingsAsync();

                var model = new ImprestItemRequisitionViewModel
                {
                    OpenRequisitions = allHeaders.Where(h => h.Status == "0" || h.Status?.ToLower() == "open").ToList(),
                    PostedRequisitions = allHeaders.Where(h => h.Status == "1" || h.Status?.ToLower() == "posted").ToList(),
                    CancelledRequisitions = allHeaders.Where(h => h.Status == "2" || h.Status?.ToLower() == "cancelled").ToList(),
                    ApprovedRequisitions = allHeaders.Where(h => h.Status == "3" || h.Status?.ToLower() == "approved").ToList(),
                    CurrentUserId = userId,
                    CurrentFilter = status,
                    ColorSettings = colorSettings,
                    Dimension1List = await _requisitionService.GetDimension1ListAsync(),
                    Dimension2List = await _requisitionService.GetDimension2ListAsync(),
                    ItemList = await _requisitionService.GetItemListAsync(),
                    GLAccountList = await _requisitionService.GetGLAccountListAsync()
                };

                // Apply status filter if specified
                if (!string.IsNullOrEmpty(status))
                {
                    if (status.Equals("Posted", StringComparison.OrdinalIgnoreCase))
                    {
                        model.OpenRequisitions = new List<ImprestItemRequisitionHeaderDto>();
                        model.CancelledRequisitions = new List<ImprestItemRequisitionHeaderDto>();
                        model.ApprovedRequisitions = new List<ImprestItemRequisitionHeaderDto>();
                    }
                    else if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                    {
                        model.OpenRequisitions = new List<ImprestItemRequisitionHeaderDto>();
                        model.PostedRequisitions = new List<ImprestItemRequisitionHeaderDto>();
                        model.ApprovedRequisitions = new List<ImprestItemRequisitionHeaderDto>();
                    }
                    else if (status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                    {
                        model.OpenRequisitions = new List<ImprestItemRequisitionHeaderDto>();
                        model.PostedRequisitions = new List<ImprestItemRequisitionHeaderDto>();
                        model.CancelledRequisitions = new List<ImprestItemRequisitionHeaderDto>();
                    }
                    else
                    {
                        model.OpenRequisitions = model.OpenRequisitions
                            .Where(h => h.Status == status || h.Status?.ToLower() == status.ToLower())
                            .ToList();
                    }
                }

                return View("~/Views/ImprestItemRequisition/Index.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading requisition index page");
                return View(new ImprestItemRequisitionViewModel
                {
                    ColorSettings = await _colorSettingsService.GetColorSettingsAsync()
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(string documentNo)
        {
            try
            {
                var userId = User.Identity.Name;
                // Get header with user filter
                var header = (await _requisitionService.GetRequisitionHeadersAsync(userId))
                    .FirstOrDefault(h => h.No == documentNo);

                if (header == null)
                {
                    return Json(new { success = false, message = "Requisition not found or access denied" });
                }

                var lines = await _requisitionService.GetRequisitionLinesAsync(documentNo);

                return Json(new
                {
                    success = true,
                    header = new
                    {
                        no = header.No,
                        requisitionType = header.RequisitionType,
                        status = header.Status,
                        requestDate = header.RequestDate,
                        postingDate = header.PostingDate,
                        requestorId = header.RequestorId,
                        departmentCode = header.DepartmentCode,
                        projectCode = header.ProjectCode,
                        description = header.Description,
                        totalAmount = header.TotalAmount
                    },
                    lines = lines.Select(l => new {
                        lineNo = l.LineNo,
                        type = l.Type,
                        no = l.No,
                        description = l.Description,
                        quantity = l.Quantity,
                        unitCost = l.UnitCost,
                        unitOfMeasure = l.UnitOfMeasure,
                        locationCode = l.LocationCode,
                        binCode = l.BinCode
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for requisition {DocumentNo}", documentNo);
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] SubmitRequisitionRequest request)
        {
            try
            {
                // Validate request.RequisitionType is either "Imprest" or "Item"
                if (request.RequisitionType != "Imprest" && request.RequisitionType != "Item")
                {
                    return BadRequest(new { Success = false, Message = "Invalid requisition type. Must be 'Imprest' or 'Item'" });
                }

                // Rest of the method remains the same
                var documentNo = await _requisitionService.SubmitRequisitionAsync(
                    User.Identity.Name,
                    request.RequisitionType,
                    request.Description,
                    request.Lines);

                return Ok(new
                {
                    Success = true,
                    Message = "Requisition submitted successfully",
                    DocumentNo = documentNo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting requisition");
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        public class SubmitRequisitionRequest
        {
            public string RequisitionType { get; set; }
            public string Description { get; set; }
            public List<ImprestItemRequisitionLineDto> Lines { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] EditRequisitionRequest request)
        {
            try
            {
                var userId = User.Identity.Name;
                var documentNo = await _requisitionService.EditRequisitionAsync(
                    userId,
                    request.DocumentNo,
                    request.PostingDate,
                    request.Description,
                    request.DepartmentCode,
                    request.ProjectCode,
                    request.Lines
                );

                return Ok(new
                {
                    Success = true,
                    Message = "Requisition updated successfully",
                    DocumentNo = documentNo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing requisition");
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(string documentNo)
        {
            try
            {
                var postedDocumentNo = await _requisitionService.PostRequisitionAsync(documentNo);

                return Ok(new
                {
                    Success = true,
                    Message = "Requisition posted successfully",
                    DocumentNo = postedDocumentNo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting requisition {DocumentNo}", documentNo);
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(string documentNo)
        {
            try
            {
                await _requisitionService.CancelRequisitionAsync(documentNo);

                return Ok(new
                {
                    Success = true,
                    Message = "Requisition cancelled successfully",
                    DocumentNo = documentNo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling requisition {DocumentNo}", documentNo);
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }
        [HttpGet]
        
        public async Task<IActionResult> GetItems()
        {
            try
            {
                var items = await _requisitionService.GetItemListAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items");
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGLAccounts()
        {
            try
            {
                var accounts = await _requisitionService.GetGLAccountListAsync();
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting GL accounts");
                return BadRequest();
            }
        }

        public class EditRequisitionRequest
        {
            public string DocumentNo { get; set; }
            public string PostingDate { get; set; }
            public string Description { get; set; }
            public string DepartmentCode { get; set; }
            public string ProjectCode { get; set; }
            public List<ImprestItemRequisitionLineDto> Lines { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> UploadDocument(string documentNo)
        {
            try
            {
                if (string.IsNullOrEmpty(documentNo))
                {
                    _logger.LogWarning("UploadDocument called with empty documentNo");
                    TempData["ErrorMessage"] = "No requisition number was specified.";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.Identity.Name;

                // Get header with user filter
                var headers = await _requisitionService.GetRequisitionHeadersAsync(userId);
                var header = headers.FirstOrDefault(h =>
                    string.Equals(h.No, documentNo, StringComparison.OrdinalIgnoreCase));

                if (header == null)
                {
                    _logger.LogWarning("Requisition {DocumentNo} not found for user {UserId}. Available requisitions: {RequisitionNumbers}",
                        documentNo, userId, string.Join(", ", headers.Select(h => h.No)));

                    TempData["ErrorMessage"] = $"The requisition {documentNo} was not found or you don't have access to it.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if requisition is still open
                if (header.Status != "0" && !header.Status.Equals("Open", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = $"Requisition {documentNo} is no longer open for document uploads (Status: {header.Status}).";
                    return RedirectToAction(nameof(Index));
                }

                var documents = await _requisitionService.GetRequisitionDocumentsAsync(documentNo);

                var model = new ImprestItemReqDocumentUploadViewModel
                {
                    RequisitionNo = documentNo,
                    Description = header.Description,
                    Status = header.Status,
                    ExistingDocuments = documents
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading upload form for requisition {DocumentNo}", documentNo);
                TempData["ErrorMessage"] = $"Error loading requisition: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(ImprestItemReqDocumentUploadViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for requisition {RequisitionNo}", model.RequisitionNo);
                    return View(model);
                }

                if (model.DocumentFile == null || model.DocumentFile.Length == 0)
                {
                    _logger.LogWarning("No file uploaded for requisition {RequisitionNo}", model.RequisitionNo);
                    ModelState.AddModelError("DocumentFile", "Please select a file to upload.");
                    return View(model);
                }

                // Validate file size (max 10MB)
                if (model.DocumentFile.Length > 10 * 1024 * 1024)
                {
                    _logger.LogWarning("File too large for requisition {RequisitionNo} ({Size} bytes)",
                        model.RequisitionNo, model.DocumentFile.Length);
                    ModelState.AddModelError("DocumentFile", "File size exceeds maximum limit of 10MB.");
                    return View(model);
                }

                using var memoryStream = new MemoryStream();
                await model.DocumentFile.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var result = await _requisitionService.UploadRequisitionDocumentAsync(
                    model.RequisitionNo,
                    model.DocumentFile.FileName,
                    fileBytes,
                    model.DocumentType,
                    model.SecurityLevel,
                    model.IsEncrypted);

                if (result)
                {
                    _logger.LogInformation("Successfully uploaded document for requisition {RequisitionNo}", model.RequisitionNo);
                    TempData["SuccessMessage"] = "Document uploaded successfully!";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogError("Failed to upload document for requisition {RequisitionNo}", model.RequisitionNo);
                ModelState.AddModelError("", "Failed to upload document. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for requisition {RequisitionNo}", model?.RequisitionNo);
                ModelState.AddModelError("", $"Error uploading document: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(string documentId, string requisitionNo)
        {
            try
            {
                var result = await _requisitionService.DeleteRequisitionDocumentAsync(documentId);
                if (result)
                {
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Failed to delete document." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(string documentId)
        {
            try
            {
                if (string.IsNullOrEmpty(documentId))
                {
                    _logger.LogWarning("Download requested with empty document ID");
                    return NotFound();
                }

                // Verify configuration exists
                if (_config == null)
                {
                    _logger.LogError("Configuration is not initialized");
                    return StatusCode(500, "Configuration error");
                }

                var companyName = _config["BusinessCentral:OData:Company"];
                if (string.IsNullOrEmpty(companyName))
                {
                    _logger.LogError("BusinessCentral:OData:Company configuration is missing");
                    return StatusCode(500, "Configuration error");
                }

                var encodedCompanyName = Uri.EscapeDataString(companyName);
                var encodedDocumentId = Uri.EscapeDataString(documentId);

                // Create the client using the factory
                var httpClient = _httpClientFactory.CreateClient("BusinessCentral");

                // Get document metadata first
                var metadataEndpoint = $"Company('{encodedCompanyName}')/ImprestItemReqDocAPI({encodedDocumentId})";

                var document = await httpClient.GetFromJsonAsync<ImprestItemReqDocumentDto>(metadataEndpoint);

                if (document == null)
                {
                    _logger.LogWarning("Document metadata not found for {DocumentId}", documentId);
                    return NotFound();
                }

                // Get the actual file content
                var contentEndpoint = $"Company('{encodedCompanyName}')/ImprestItemReqDocAPI({encodedDocumentId})/fileContent";
                var response = await httpClient.GetAsync(contentEndpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download content for document {DocumentId}. Status: {StatusCode}",
                        documentId, response.StatusCode);
                    return NotFound();
                }

                var fileContent = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? GetContentType(document.FileExtension);

                _logger.LogInformation("Successfully downloaded document {DocumentId}", documentId);
                return File(fileContent, contentType, $"{document.FileName}.{document.FileExtension}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
                TempData["ErrorMessage"] = "An error occurred while downloading the document. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        private string GetContentType(string fileExtension)
        {
            if (string.IsNullOrEmpty(fileExtension))
            {
                return "application/octet-stream";
            }

            return fileExtension.ToLower() switch
            {
                "pdf" => "application/pdf",
                "doc" => "application/msword",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "xls" => "application/vnd.ms-excel",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetDocumentsCount(string requisitionNo)
        {
            try
            {
                var documents = await _requisitionService.GetRequisitionDocumentsAsync(requisitionNo);
                return Ok(documents.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document count for requisition {RequisitionNo}", requisitionNo);
                return Ok(0);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDocuments(string requisitionNo)
        {
            try
            {
                var userId = User.Identity.Name;

                // Verify user has access to this requisition
                var headers = await _requisitionService.GetRequisitionHeadersAsync(userId);
                if (!headers.Any(h => h.No == requisitionNo))
                {
                    return Json(new { success = false, message = "Access denied to this requisition" });
                }

                var documents = await _requisitionService.GetRequisitionDocumentsAsync(requisitionNo);

                return Json(new
                {
                    success = true,
                    documents = documents.Select(d => new {
                        systemId = d.SystemId,
                        fileName = d.FileName,
                        fileExtension = d.FileExtension,
                        documentType = d.DocumentType,
                        uploadedDate = d.UploadedDate,
                        requisitionNo = d.RequisitionNo
                    }),
                    isOpen = headers.First(h => h.No == requisitionNo).Status == "0"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents for requisition {RequisitionNo}", requisitionNo);
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}