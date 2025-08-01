using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecruitmentPortal.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.ServiceModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TenderDocumentAPI;
using Microsoft.AspNetCore.Http;

namespace RecruitmentPortal.Services
{
    public class BusinessCentralTenderService : IBusinessCentralTenderService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly TenderDocumentAPI_PortClient _soapClient;
        private readonly ILogger<BusinessCentralTenderService> _logger;

        public BusinessCentralTenderService(
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            ILogger<BusinessCentralTenderService> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = httpClientFactory.CreateClient("BusinessCentral");
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;

            // Initialize SOAP client
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportCredentialOnly)
            {
                Security = {
                    Transport = {
                        ClientCredentialType = HttpClientCredentialType.Basic,
                        ProxyCredentialType = HttpProxyCredentialType.None
                    }
                },
                MaxBufferSize = int.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                AllowCookies = true
            };

            var endpoint = _config["BusinessCentral:SOAP:TenderEndpoint"];
            _soapClient = new TenderDocumentAPI_PortClient(binding, new EndpointAddress(endpoint));
            _soapClient.ClientCredentials.UserName.UserName = _config["BusinessCentral:SOAP:Username"];
            _soapClient.ClientCredentials.UserName.Password = _config["BusinessCentral:SOAP:Password"];
        }

        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        public async Task<List<TenderDto>> GetPublishedTendersAsync(string vendorNo = null)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var endpoint = $"Company('{encodedCompanyName}')/TenderDocumentsAPI?$expand=TenderDocumentsAPIattachments";

                if (!string.IsNullOrEmpty(vendorNo))
                {
                    var vendorCategories = await GetRfqVendorCategoriesByVendorAsync(vendorNo);
                    if (vendorCategories.Any())
                    {
                        var categoryFilters = string.Join(" or ",
                            vendorCategories.Select(c => $"categoryCode eq '{c.CategoryCode}'"));
                        endpoint += $"&$filter=({categoryFilters})";
                    }
                }

                _logger.LogDebug("Fetching published tenders for vendor {VendorNo}", vendorNo);
                var response = await _httpClient.GetFromJsonAsync<ODataResponse<TenderDto>>(endpoint);
                var tenders = response?.Value ?? new List<TenderDto>();

                // Filter out closed tenders
                var activeTenders = tenders.Where(t => t.ClosingDate >= DateTime.Today).ToList();
                _logger.LogInformation("Found {Count} active tenders for vendor {VendorNo}", activeTenders.Count, vendorNo);

                return activeTenders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting published tenders for vendor {VendorNo}", vendorNo);
                throw;
            }
        }

        public async Task<TenderDto> GetTenderDocumentAsync(string systemId)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var encodedSystemId = Uri.EscapeDataString(systemId);
                var endpoint = $"Company('{encodedCompanyName}')/TenderDocumentsAPI({encodedSystemId})?$expand=TenderDocumentsAPIattachments";

                _logger.LogDebug("Fetching tender document {SystemId}", systemId);
                using (var response = await _httpClient.GetAsync(endpoint))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("API returned {StatusCode} for tender {SystemId}", response.StatusCode, systemId);
                        return null;
                    }

                    var content = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var tender = await response.Content.ReadFromJsonAsync<TenderDto>();
                        if (tender != null)
                        {
                            _logger.LogDebug("Successfully deserialized tender {SystemId}", systemId);
                            return tender;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Direct deserialization failed for tender {SystemId}", systemId);
                    }

                    try
                    {
                        var result = await response.Content.ReadFromJsonAsync<ODataResponse<TenderDto>>();
                        var tender = result?.Value?.FirstOrDefault();

                        if (tender != null)
                        {
                            _logger.LogDebug("Successfully deserialized tender {SystemId} via ODataResponse", systemId);
                        }
                        else
                        {
                            _logger.LogWarning("No tender data found for {SystemId}", systemId);
                        }

                        return tender;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "ODataResponse deserialization failed for tender {SystemId}", systemId);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tender document {SystemId}", systemId);
                throw;
            }
        }

        public async Task<TenderDto> GetTenderByIdAsync(Guid id)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var encodedId = Uri.EscapeDataString(id.ToString());
                var endpoint = $"Company('{encodedCompanyName}')/TenderDocumentsAPI({encodedId})?$expand=TenderDocumentsAPIattachments";

                _logger.LogDebug("Fetching tender by ID: {TenderId}", id);
                var response = await _httpClient.GetFromJsonAsync<TenderDto>(endpoint);

                if (response == null)
                {
                    _logger.LogWarning("Tender {TenderId} not found", id);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tender by ID {TenderId}", id);
                throw;
            }
        }

        public async Task<(int Open, int Closed, int Awarded)> GetTenderCountsByStatusAsync(string vendorNo = null)
        {
            try
            {
                var tenders = await GetPublishedTendersAsync(vendorNo);

                int openCount = tenders.Count(t => t.Status.Equals("Open", StringComparison.OrdinalIgnoreCase));
                int closedCount = tenders.Count(t => t.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase));
                int awardedCount = tenders.Count(t => t.Status.Equals("Awarded", StringComparison.OrdinalIgnoreCase));

                return (openCount, closedCount, awardedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tender counts by status for vendor {VendorNo}", vendorNo);
                return (0, 0, 0);
            }
        }

        public async Task<List<TenderAttachmentDto>> GetTenderAttachmentsAsync(Guid tenderId)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var encodedTenderId = Uri.EscapeDataString(tenderId.ToString());
                var endpoint = $"Company('{encodedCompanyName}')/TenderDocumentsAPI({encodedTenderId})/TenderDocumentsAPIattachments";

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<TenderAttachmentDto>>(endpoint);
                return response?.Value ?? new List<TenderAttachmentDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attachments for tender {TenderId}", tenderId);
                throw;
            }
        }

        public async Task<bool> UploadTenderDocumentAsync(
            string tenderNo,
            string fileName,
            byte[] fileContent,
            string documentType = "Other",
            string securityLevel = "Internal",
            bool isEncrypted = false,
            string vendorNo = null)
        {
            try
            {
                _logger.LogInformation("Uploading document {FileName} for tender {TenderNo}", fileName, tenderNo);

                var requestBody = new UploadTenderDocumentBody(
                    tenderNo,
                    fileName,
                    Convert.ToBase64String(fileContent),
                    documentType,
                    securityLevel,
                    isEncrypted,
                    User?.Identity?.Name ?? "System",
                    vendorNo);

                var request = new UploadTenderDocument(requestBody);
                var response = await _soapClient.UploadTenderDocumentAsync(request);

                if (response?.Body == null)
                {
                    _logger.LogError("Empty response received when uploading document for tender {TenderNo}", tenderNo);
                    return false;
                }

                _logger.LogInformation("Successfully uploaded document {FileName} for tender {TenderNo}", fileName, tenderNo);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document {FileName} for tender {TenderNo}", fileName, tenderNo);
                throw new Exception($"Error uploading document: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteTenderAttachmentAsync(string attachmentId)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var encodedAttachmentId = Uri.EscapeDataString(attachmentId);
                var endpoint = $"Company('{encodedCompanyName}')/TenderDocumentsAPIattachments({encodedAttachmentId})";

                var response = await _httpClient.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment {AttachmentId}", attachmentId);
                throw;
            }
        }

        public async Task<List<TenderDocumentDto>> GetTenderDocumentsAsync(Guid tenderId, string vendorNo = null)
        {
            try
            {
                var tender = await GetTenderByIdAsync(tenderId);
                if (tender == null)
                {
                    _logger.LogWarning("Tender {TenderId} not found", tenderId);
                    return new List<TenderDocumentDto>();
                }

                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var filter = $"tenderNo eq '{tender.TenderNo}'";

                if (!string.IsNullOrEmpty(vendorNo))
                {
                    filter += $" and vendorNo eq '{vendorNo}'";
                }

                var encodedFilter = Uri.EscapeDataString(filter);
                var endpoint = $"Company('{encodedCompanyName}')/TenderDocResponsesAPI?$filter={encodedFilter}";

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<TenderDocumentDto>>(endpoint);
                return response?.Value ?? new List<TenderDocumentDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tender documents for tender ID {TenderId}", tenderId);
                return new List<TenderDocumentDto>();
            }
        }

        public async Task<bool> DeleteTenderDocumentAsync(string documentId)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var encodedDocumentId = Uri.EscapeDataString(documentId);
                var endpoint = $"Company('{encodedCompanyName}')/TenderDocResponsesAPI({encodedDocumentId})";

                var response = await _httpClient.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tender document {DocumentId}", documentId);
                throw;
            }
        }

        private async Task<List<RfqVendorCategoryDto>> GetRfqVendorCategoriesByVendorAsync(string vendorNo)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var encodedFilter = Uri.EscapeDataString($"VendorNo eq '{vendorNo}'");
                var endpoint = $"Company('{encodedCompanyName}')/RFQVendorCategoriesAPI?$filter={encodedFilter}";

                _logger.LogDebug("Fetching vendor categories for vendor {VendorNo}", vendorNo);
                var response = await _httpClient.GetFromJsonAsync<ODataResponse<RfqVendorCategoryDto>>(endpoint);
                var categories = response?.Value ?? new List<RfqVendorCategoryDto>();

                _logger.LogInformation("Found {Count} categories for vendor {VendorNo}", categories.Count, vendorNo);
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendor categories for vendor {VendorNo}", vendorNo);
                throw;
            }
        }
    }
}