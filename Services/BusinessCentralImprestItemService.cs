using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecruitmentPortal.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Linq;

namespace RecruitmentPortal.Services
{
    public class BusinessCentralImprestItemService : IBusinessCentralImprestItemService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<BusinessCentralImprestItemService> _logger;

        public BusinessCentralImprestItemService(
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ILogger<BusinessCentralImprestItemService> logger)
        {
            _config = config;
            _httpClient = httpClientFactory.CreateClient("BusinessCentral");
            _logger = logger;
        }

        public async Task<List<ImprestItemRequisitionHeaderDto>> GetRequisitionHeadersAsync(string userId = null)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var endpoint = $"Company('{encodedCompanyName}')/ImprestItemReqHeaderAPI";

                // Modified filter to include all relevant statuses
                if (!string.IsNullOrEmpty(userId))
                {
                    endpoint += $"?$filter=RequestorId eq '{userId}'";
                }
                else
                {
                    endpoint += "?$filter=Status eq 'Open' or Status eq '0'";
                }

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<ImprestItemRequisitionHeaderDto>>(endpoint);
                return response?.Value ?? new List<ImprestItemRequisitionHeaderDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requisition headers for user {UserId}", userId);
                return new List<ImprestItemRequisitionHeaderDto>();
            }
        }

        public async Task<List<ImprestItemRequisitionLineDto>> GetRequisitionLinesAsync(string documentNo)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var encodedFilter = Uri.EscapeDataString($"DocumentNo eq '{documentNo}'");
                var endpoint = $"Company('{encodedCompanyName}')/ImprestItemReqLineAPI?$filter={encodedFilter}";

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<ImprestItemRequisitionLineDto>>(endpoint);
                return response?.Value ?? new List<ImprestItemRequisitionLineDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requisition lines for document {DocumentNo}", documentNo);
                return new List<ImprestItemRequisitionLineDto>();
            }
        }

        public async Task<string> SubmitRequisitionAsync(
            string userId,
            string requisitionType,
            string description,
            List<ImprestItemRequisitionLineDto> lines)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);

                // === Step 1: Create the Requisition Header ===
                var headerEndpoint = $"Company('{encodedCompanyName}')/ImprestItemReqHeaderAPI";

                var headerData = new
                {
                    requisitionType = requisitionType,
                    reqTypeAPI = requisitionType,
                    description = description,
                    shortcutDimension1Code = lines.FirstOrDefault()?.ShortcutDimension1Code,
                    shortcutDimension2Code = lines.FirstOrDefault()?.ShortcutDimension2Code,
                    postingDate = DateTime.Today.ToString("yyyy-MM-dd"),
                    requestorId = userId
                };

                var headerResponse = await _httpClient.PostAsJsonAsync(headerEndpoint, headerData);
                headerResponse.EnsureSuccessStatusCode();

                // Extract the created document number
                var createdHeader = await headerResponse.Content.ReadFromJsonAsync<ImprestItemRequisitionHeaderDto>();
                var documentNo = createdHeader.No;

                // === Step 2: Add Lines to the Requisition ===
                var lineEndpoint = $"Company('{encodedCompanyName}')/ImprestItemReqLineAPI";

                foreach (var line in lines)
                {
                    // Validate line type matches header type
                    if (requisitionType == "Imprest" && line.Type != "G/L Account")
                    {
                        throw new Exception("All lines must be G/L Accounts for Imprest requisitions");
                    }
                    else if (requisitionType == "Item" && line.Type != "Item")
                    {
                        throw new Exception("All lines must be Items for Item requisitions");
                    }

                    var lineData = new
                    {
                        documentNo = documentNo,
                        type = line.Type,
                        no = line.No,
                        description = line.Description,
                        quantity = line.Quantity,
                        unitOfMeasure = line.Type == "G/L Account" ? string.Empty : line.UnitOfMeasure,
                        unitCost = line.UnitCost,
                        locationCode = line.LocationCode,
                        binCode = line.BinCode,
                        shortcutDimension1Code = line.ShortcutDimension1Code,
                        shortcutDimension2Code = line.ShortcutDimension2Code
                    };

                    var lineResponse = await _httpClient.PostAsJsonAsync(lineEndpoint, lineData);
                    if (!lineResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await lineResponse.Content.ReadAsStringAsync();
                        throw new Exception($"Failed to create line: {errorContent}");
                    }
                }

                return documentNo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting requisition for user {UserId}", userId);
                throw;
            }
        }

        public async Task<string> EditRequisitionAsync(
            string userId,
            string documentNo,
            string postingDate,
            string description,
            string departmentCode,
            string projectCode,
            List<ImprestItemRequisitionLineDto> lines)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);

                // === Step 1: Get the header with filter (works around URL issues) ===
                var filter = $"$filter=No eq '{documentNo.Replace("'", "''")}'";
                var getHeaderEndpoint = $"Company('{encodedCompanyName}')/ImprestItemReqHeaderAPI?{filter}";

                var headerResponse = await _httpClient.GetFromJsonAsync<ODataResponse<ImprestItemRequisitionHeaderDto>>(getHeaderEndpoint);
                var header = headerResponse?.Value?.FirstOrDefault();

                if (header == null)
                {
                    throw new Exception($"Requisition {documentNo} not found");
                }

                // === Step 2: Update the header using SystemId as key ===
                var patchEndpoint = $"Company('{encodedCompanyName}')/ImprestItemReqHeaderAPI({header.SystemId})";

                var headerData = new Dictionary<string, object>
                {
                    ["postingDate"] = postingDate,
                    ["description"] = description,
                    ["shortcutDimension1Code"] = departmentCode,
                    ["shortcutDimension2Code"] = projectCode
                };

                var request = new HttpRequestMessage(HttpMethod.Patch, patchEndpoint)
                {
                    Content = JsonContent.Create(headerData)
                };

                request.Headers.Add("If-Match", "*");
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var patchResponse = await _httpClient.SendAsync(request);
                if (!patchResponse.IsSuccessStatusCode)
                {
                    var errorContent = await patchResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to update header: {errorContent}");
                }

                // === Step 3: Delete existing lines ===
                var existingLines = await GetRequisitionLinesAsync(documentNo);
                foreach (var line in existingLines)
                {
                    var deleteEndpoint = $"Company('{encodedCompanyName}')/ImprestItemReqLineAPI({line.SystemId})";
                    var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, deleteEndpoint);
                    deleteRequest.Headers.Add("If-Match", "*");

                    var deleteResponse = await _httpClient.SendAsync(deleteRequest);
                    if (!deleteResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await deleteResponse.Content.ReadAsStringAsync();
                        _logger.LogWarning($"Failed to delete line {line.LineNo}. Error: {errorContent}");
                    }
                }

                // === Step 4: Add new lines ===
                var lineEndpoint = $"Company('{encodedCompanyName}')/ImprestItemReqLineAPI";
                foreach (var line in lines)
                {
                    var lineData = new
                    {
                        documentNo = documentNo,
                        type = line.Type,
                        no = line.No,
                        description = line.Description,
                        quantity = line.Quantity,
                        unitOfMeasure = line.UnitOfMeasure,
                        unitCost = line.UnitCost,
                        locationCode = line.LocationCode,
                        binCode = line.BinCode,
                        shortcutDimension1Code = line.ShortcutDimension1Code,
                        shortcutDimension2Code = line.ShortcutDimension2Code
                    };

                    var lineResponse = await _httpClient.PostAsJsonAsync(lineEndpoint, lineData);
                    lineResponse.EnsureSuccessStatusCode();
                }

                return documentNo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing requisition {DocumentNo}", documentNo);
                throw new Exception($"Error editing requisition: {ex.Message}", ex);
            }
        }

        public async Task<string> PostRequisitionAsync(string documentNo)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var endpoint = $"Company('{encodedCompanyName}')/ImprestItemReqPostAPI";

                var response = await _httpClient.PostAsJsonAsync(endpoint, new { documentNo });
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<PostRequisitionResult>();
                return result?.PostedDocumentNo ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting requisition {DocumentNo}", documentNo);
                throw;
            }
        }

        public async Task<string> CancelRequisitionAsync(string documentNo)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var endpoint = $"Company('{encodedCompanyName}')/ImprestItemReqCancelAPI";

                var response = await _httpClient.PostAsJsonAsync(endpoint, new { documentNo });
                response.EnsureSuccessStatusCode();

                return documentNo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling requisition {DocumentNo}", documentNo);
                throw;
            }
        }

        public async Task<List<Dimension1Dto>> GetDimension1ListAsync()
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var endpoint = $"Company('{encodedCompanyName}')/Dimension1API";

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<Dimension1Dto>>(endpoint);
                return response?.Value ?? new List<Dimension1Dto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Dimension 1 list");
                return new List<Dimension1Dto>();
            }
        }

        public async Task<List<Dimension2Dto>> GetDimension2ListAsync()
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var endpoint = $"Company('{encodedCompanyName}')/Dimension2API";

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<Dimension2Dto>>(endpoint);
                return response?.Value ?? new List<Dimension2Dto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Dimension 2 list");
                return new List<Dimension2Dto>();
            }
        }

        public async Task<List<ItemDto>> GetItemListAsync()
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var endpoint = $"Company('{encodedCompanyName}')/ItemsListAPI";

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<ItemDto>>(endpoint);
                return response?.Value ?? new List<ItemDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Item list");
                return new List<ItemDto>();
            }
        }

        public async Task<List<GLAccountDto>> GetGLAccountListAsync()
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var endpoint = $"Company('{encodedCompanyName}')/GLAccountsListAPI";

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<GLAccountDto>>(endpoint);
                return response?.Value ?? new List<GLAccountDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting G/L Account list");
                return new List<GLAccountDto>();
            }
        }

        public async Task<List<ImprestItemReqDocumentDto>> GetRequisitionDocumentsAsync(string documentNo)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var encodedFilter = Uri.EscapeDataString($"requisitionNo eq '{documentNo}'");
                var endpoint = $"Company('{encodedCompanyName}')/ImprestItemReqDocAPI?$filter={encodedFilter}";

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<ImprestItemReqDocumentDto>>(endpoint);
                return response?.Value ?? new List<ImprestItemReqDocumentDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents for requisition {DocumentNo}", documentNo);
                return new List<ImprestItemReqDocumentDto>();
            }
        }

        public async Task<bool> UploadRequisitionDocumentAsync(
            string requisitionNo,
            string fileName,
            byte[] fileContent,
            string documentType = "Other",
            string securityLevel = "Internal",
            bool isEncrypted = false,
            string uploadedBy = null)
        {
            try
            {
                _logger.LogInformation("Uploading document {FileName} for requisition {RequisitionNo}", fileName, requisitionNo);

                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var endpoint = $"Company('{encodedCompanyName}')/ImprestItemReqDocAPI";

                var document = new
                {
                    requisitionNo = requisitionNo,
                    fileName = fileName,
                    fileContent = Convert.ToBase64String(fileContent),
                    documentType = documentType,
                    securityLevel = securityLevel,
                    isEncrypted = isEncrypted,
                    uploadedBy = uploadedBy ?? "System"
                };

                var response = await _httpClient.PostAsJsonAsync(endpoint, document);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to upload document for requisition {RequisitionNo}. Status: {StatusCode}",
                        requisitionNo, response.StatusCode);
                    return false;
                }

                _logger.LogInformation("Successfully uploaded document {FileName} for requisition {RequisitionNo}", fileName, requisitionNo);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document {FileName} for requisition {RequisitionNo}", fileName, requisitionNo);
                throw new Exception($"Error uploading document: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteRequisitionDocumentAsync(string documentId)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var encodedDocumentId = Uri.EscapeDataString(documentId);
                var endpoint = $"Company('{encodedCompanyName}')/ImprestItemReqDocAPI({encodedDocumentId})";

                var response = await _httpClient.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting requisition document {DocumentId}", documentId);
                throw;
            }
        }

        private class SubmitRequisitionResult
        {
            public string DocumentNo { get; set; }
        }

        private class PostRequisitionResult
        {
            public string PostedDocumentNo { get; set; }
        }

        public class ODataResponse<T>
        {
            public List<T> Value { get; set; }
        }
    }
}