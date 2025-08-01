using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecruitmentPortal.Models.DTOs;
using RecruitmentPortal.Models.ViewModels;
using RecruitmentPortal.Services;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RecruitmentPortal.Controllers
{
    [Authorize]
    public class TenderController : Controller
    {
        private readonly IBusinessCentralTenderService _tenderService;
        private readonly BusinessCentralAuthService _authService;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<TenderController> _logger;

        public TenderController(
            IBusinessCentralTenderService tenderService,
            BusinessCentralAuthService authService,
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ILogger<TenderController> logger)
        {
            _tenderService = tenderService;
            _authService = authService;
            _config = config;
            _httpClient = httpClientFactory.CreateClient("BusinessCentral");
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get current user's vendor number
                var vendorNo = await _authService.GetVendorNoByEmailAsync(User.Identity.Name);
                if (string.IsNullOrEmpty(vendorNo))
                {
                    _logger.LogWarning("No vendor assigned to user {User}", User.Identity.Name);
                    TempData["ErrorMessage"] = "Your account is not associated with a vendor. Please contact support.";
                    return View(new TenderViewModel());
                }

                var tenders = await _tenderService.GetPublishedTendersAsync(vendorNo);
                var (openCount, closedCount, awardedCount) = await _tenderService.GetTenderCountsByStatusAsync(vendorNo);

                var model = new TenderViewModel
                {
                    Tenders = tenders,
                    CurrentVendorNo = vendorNo,
                    OpenTenderCount = openCount,
                    ClosedTenderCount = closedCount,
                    AwardedTenderCount = awardedCount
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tender index page");
                TempData["ErrorMessage"] = "An error occurred while loading tenders. Please try again later.";
                return View(new TenderViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Download(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Download requested with empty ID");
                    return NotFound();
                }

                var tender = await _tenderService.GetTenderDocumentAsync(id);
                if (tender == null || tender.Attachments == null || !tender.Attachments.Any())
                {
                    _logger.LogWarning("No attachments found for tender {TenderId}", id);
                    return NotFound();
                }

                // For simplicity, we'll download the first attachment
                var attachment = tender.Attachments.First();
                var fileContent = Convert.FromBase64String(attachment.FileContent);

                _logger.LogInformation("Downloading attachment {AttachmentId} for tender {TenderId}", attachment.SystemId, id);
                return File(fileContent, "application/octet-stream", $"{attachment.FileName}.{attachment.FileExtension}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading tender document {TenderId}", id);
                TempData["ErrorMessage"] = "An error occurred while downloading the document. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(string attachmentId)
        {
            try
            {
                if (string.IsNullOrEmpty(attachmentId))
                {
                    _logger.LogWarning("Download requested with empty attachment ID");
                    return NotFound();
                }

                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var encodedAttachmentId = Uri.EscapeDataString(attachmentId);
                var endpoint = $"Company('{encodedCompanyName}')/TenderDocumentsAPIattachments({encodedAttachmentId})";

                var attachment = await _httpClient.GetFromJsonAsync<TenderAttachmentDto>(endpoint);
                if (attachment == null || string.IsNullOrEmpty(attachment.FileContent))
                {
                    _logger.LogWarning("Attachment {AttachmentId} not found or has no content", attachmentId);
                    return NotFound();
                }

                _logger.LogInformation("Downloading attachment {AttachmentId}", attachmentId);
                var fileContent = Convert.FromBase64String(attachment.FileContent);
                return File(fileContent, "application/octet-stream", $"{attachment.FileName}.{attachment.FileExtension}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading attachment {AttachmentId}", attachmentId);
                TempData["ErrorMessage"] = "An error occurred while downloading the attachment. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> UploadDocument(Guid tenderId)
        {
            try
            {
                var vendorNo = await _authService.GetVendorNoByEmailAsync(User.Identity.Name);
                if (string.IsNullOrEmpty(vendorNo))
                {
                    _logger.LogWarning("No vendor assigned to user {User}", User.Identity.Name);
                    TempData["ErrorMessage"] = "Your account is not associated with a vendor. Please contact support.";
                    return RedirectToAction(nameof(Index));
                }

                var tender = await _tenderService.GetTenderByIdAsync(tenderId);
                if (tender == null)
                {
                    _logger.LogWarning("Tender {TenderId} not found", tenderId);
                    TempData["ErrorMessage"] = "The requested tender was not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if tender is still open
                if (tender.ClosingDate < DateTime.Today)
                {
                    TempData["ErrorMessage"] = "This tender has already closed and no longer accepts submissions.";
                    return RedirectToAction(nameof(Index));
                }

                // Get documents filtered by vendor
                var documents = await _tenderService.GetTenderDocumentsAsync(tenderId, vendorNo);

                var model = new TenderDocumentUploadViewModel
                {
                    TenderId = tenderId,
                    TenderNo = tender.TenderNo,
                    Description = tender.Description,
                    Status = tender.Status,
                    VendorNo = vendorNo, // Add vendor number to the model
                    ExistingDocuments = documents
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading upload form for tender {TenderId}", tenderId);
                TempData["ErrorMessage"] = $"Error loading tender: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTenderDocument(string documentId, Guid tenderId)
        {
            try
            {
                var result = await _tenderService.DeleteTenderDocumentAsync(documentId);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(TenderDocumentUploadViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for tender {TenderNo}", model.TenderNo);
                    return View(model);
                }

                if (model.DocumentFile == null || model.DocumentFile.Length == 0)
                {
                    _logger.LogWarning("No file uploaded for tender {TenderNo}", model.TenderNo);
                    ModelState.AddModelError("DocumentFile", "Please select a file to upload.");
                    return View(model);
                }

                // Validate file size (max 10MB)
                if (model.DocumentFile.Length > 10 * 1024 * 1024)
                {
                    _logger.LogWarning("File too large for tender {TenderNo} ({Size} bytes)", model.TenderNo, model.DocumentFile.Length);
                    ModelState.AddModelError("DocumentFile", "File size exceeds maximum limit of 10MB.");
                    return View(model);
                }

                using var memoryStream = new MemoryStream();
                await model.DocumentFile.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var result = await _tenderService.UploadTenderDocumentAsync(
                    model.TenderNo,
                    model.DocumentFile.FileName,
                    fileBytes,
                    model.DocumentType,
                    model.SecurityLevel,
                    model.IsEncrypted,
                    model.VendorNo); // Use the VendorNo from the model

                if (result)
                {
                    _logger.LogInformation("Successfully uploaded document for tender {TenderNo}", model.TenderNo);
                    TempData["SuccessMessage"] = "Document uploaded successfully!";
                    return RedirectToAction("Index");
                }

                _logger.LogError("Failed to upload document for tender {TenderNo}", model.TenderNo);
                ModelState.AddModelError("", "Failed to upload document. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for tender {TenderNo}", model?.TenderNo);
                ModelState.AddModelError("", $"Error uploading document: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(string attachmentId, Guid tenderId)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var encodedAttachmentId = Uri.EscapeDataString(attachmentId);
                var endpoint = $"Company('{encodedCompanyName}')/TenderDocumentsAPIattachments({encodedAttachmentId})";

                var response = await _httpClient.DeleteAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to delete attachment {AttachmentId}. Status: {StatusCode}",
                        attachmentId, response.StatusCode);
                    return Json(new { success = false, message = "Failed to delete attachment." });
                }

                _logger.LogInformation("Successfully deleted attachment {AttachmentId}", attachmentId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment {AttachmentId}", attachmentId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTenderDocument(string documentId)
        {
            try
            {
                if (string.IsNullOrEmpty(documentId))
                {
                    _logger.LogWarning("Download requested with empty document ID");
                    return NotFound();
                }

                // Get document metadata first
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var encodedDocumentId = Uri.EscapeDataString(documentId);
                var metadataEndpoint = $"Company('{encodedCompanyName}')/TenderDocResponsesAPI({encodedDocumentId})";

                var document = await _httpClient.GetFromJsonAsync<TenderDocumentDto>(metadataEndpoint);
                if (document == null)
                {
                    _logger.LogWarning("Document metadata not found for {DocumentId}", documentId);
                    return NotFound();
                }

                // Get the actual file content from the content endpoint
                var contentEndpoint = $"Company('{encodedCompanyName}')/TenderDocResponsesAPI({encodedDocumentId})/fileContent";
                var response = await _httpClient.GetAsync(contentEndpoint);

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
    }
}