using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecruitmentPortal.Models.DTOs;
using RFQVendorManage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.ServiceModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RecruitmentPortal.Services
{
    public class BusinessCentralRfqService : IBusinessCentralRfqService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly RFQVendorPortalAPI_PortClient _soapClient;
        private readonly ILogger<BusinessCentralRfqService> _logger;

        public BusinessCentralRfqService(
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ILogger<BusinessCentralRfqService> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = httpClientFactory.CreateClient("BusinessCentral");
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

            var endpoint = _config["BusinessCentral:SOAP:RFQEndpoint"];
            _soapClient = new RFQVendorPortalAPI_PortClient(binding, new EndpointAddress(endpoint));
            _soapClient.ClientCredentials.UserName.UserName = _config["BusinessCentral:SOAP:Username"];
            _soapClient.ClientCredentials.UserName.Password = _config["BusinessCentral:SOAP:Password"];
        }

        public async Task<List<RfqPublishedLineDto>> GetPublishedRfqLinesAsync(string vendorNo = null)
        {
            try
            {
                decimal defaultVatPercentage = await GetDefaultVatPercentageAsync();

                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var endpoint = $"Company('{encodedCompanyName}')/RFQPublishedLinesAPI";

                if (!string.IsNullOrEmpty(vendorNo))
                {
                    var vendorCategories = await GetRfqVendorCategoriesByVendorAsync(vendorNo);
                    if (vendorCategories.Any())
                    {
                        var categoryFilters = string.Join(" or ",
                            vendorCategories.Select(c => $"CategoryCode eq '{c.CategoryCode}'"));
                        endpoint += $"?$filter={Uri.EscapeDataString(categoryFilters)}";
                    }
                }

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<RfqPublishedLineDto>>(endpoint);

                var lines = response?.Value ?? new List<RfqPublishedLineDto>();

                // Get all quoted line IDs for this vendor
                var quotedLineIds = await GetQuotedLineIdsAsync(vendorNo);

                foreach (var line in lines)
                {
                    line.DirectUnitCost = line.DirectUnitCost;
                    line.Specifications = line.Specifications ?? string.Empty;
                    line.AdditionalNotes = line.AdditionalNotes ?? string.Empty;
                    line.RfqStatus = line.RfqStatus ?? "0";
                    line.AwardedToVendorNo = line.AwardedToVendorNo ?? string.Empty;

                    // Set IsAlreadyQuoted based on quotedLineIds
                    line.IsAlreadyQuoted = quotedLineIds.Contains(line.SystemIdGuid);

                    // For submitted lines, get the actual quoted values
                    if (line.IsAlreadyQuoted)
                    {
                        var quoteDetails = await GetQuoteDetailsForLine(line.SystemId, vendorNo);
                        if (quoteDetails != null)
                        {
                            line.DirectUnitCost = quoteDetails.SubmittedPrice;
                            line.VatOption = quoteDetails.VatOption;
                            line.VatPercentage = quoteDetails.VatPercentage;
                            line.PriceIncludesVAT = quoteDetails.PriceIncludesVAT;
                        }
                    }
                    else
                    {
                        // Set default values for non-submitted lines
                        line.VatOption = "Vatable";
                        line.VatPercentage = defaultVatPercentage;
                        line.PriceIncludesVAT = false;
                    }
                }
                return lines;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting published RFQ lines for vendor {VendorNo}", vendorNo);
                return new List<RfqPublishedLineDto>();
            }
        }

        private async Task<QuoteDetailsDto> GetQuoteDetailsForLine(string systemId, string vendorNo)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var endpoint = $"Company('{encodedCompanyName}')/RFQVendorQuotesAPI?$filter=prequalifiedSupplierNo eq '{vendorNo}' and LineSystemId eq {systemId}";

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<QuoteDetailsDto>>(endpoint);
                return response?.Value?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quote details for line {SystemId} and vendor {VendorNo}", systemId, vendorNo);
                return null;
            }
        }

        public class QuoteDetailsDto
        {
            [JsonPropertyName("LineSystemId")]
            public string LineSystemId { get; set; }

            [JsonPropertyName("submittedPrice")]
            public decimal SubmittedPrice { get; set; }

            [JsonPropertyName("vatOption")]
            public string VatOption { get; set; }

            [JsonPropertyName("vatPercentage")]
            public decimal VatPercentage { get; set; }

            [JsonPropertyName("priceIncludesVAT")]
            public bool PriceIncludesVAT { get; set; }
        }

        public async Task<List<RfqVendorCategoryDto>> GetRfqVendorCategoriesAsync()
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var endpoint = $"Company('{encodedCompanyName}')/RFQVendorCategoriesAPI";

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<RfqVendorCategoryDto>>(endpoint);
                return response?.Value ?? new List<RfqVendorCategoryDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting RFQ vendor categories");
                return new List<RfqVendorCategoryDto>();
            }
        }

        public async Task<List<RfqVendorCategoryDto>> GetRfqVendorCategoriesByVendorAsync(string vendorNo)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var encodedFilter = Uri.EscapeDataString($"VendorNo eq '{vendorNo}'");
                var endpoint = $"Company('{encodedCompanyName}')/RFQVendorCategoriesAPI?$filter={encodedFilter}";

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<RfqVendorCategoryDto>>(endpoint);
                return response?.Value ?? new List<RfqVendorCategoryDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting RFQ vendor categories for vendor {VendorNo}", vendorNo);
                return new List<RfqVendorCategoryDto>();
            }
        }

        public async Task<string> SubmitQuoteAsync(string vendorNo, string itemNo, decimal unitCost, bool priceIncludesVAT, string vatOption, decimal vatPercentage, decimal quantity, Guid systemId2)
        {
            try
            {
                // Get the specific RFQ line we're quoting using systemId2
                var rfqLines = await GetPublishedRfqLinesAsync(vendorNo);
                var lineToQuote = rfqLines.FirstOrDefault(x => x.SystemIdGuid == systemId2);

                if (lineToQuote == null)
                {
                    throw new Exception($"No published RFQ line found with SystemId {systemId2}");
                }

                // Create submission model with both IDs
                var quote = new QuoteSubmissionModel
                {
                    SystemId = Guid.Parse(lineToQuote.SystemId),
                    SystemId2 = systemId2,
                    ItemNo = itemNo,
                    UnitCost = unitCost,
                    PriceIncludesVAT = priceIncludesVAT,
                    VatOption = vatOption,
                    VatPercentage = vatPercentage,
                    Quantity = quantity
                };

                // Submit as single quote
                return await SubmitMultipleQuotesAsync(vendorNo, new List<QuoteSubmissionModel> { quote });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting quote for vendor {VendorNo}, item {ItemNo}", vendorNo, itemNo);
                throw;
            }
        }

        public async Task<string> SubmitMultipleQuotesAsync(string vendorNo, List<QuoteSubmissionModel> quotes)
        {
            try
            {
                if (string.IsNullOrEmpty(vendorNo))
                {
                    throw new ArgumentException("Vendor number is required", nameof(vendorNo));
                }

                if (quotes == null || !quotes.Any())
                {
                    throw new ArgumentException("At least one quote is required", nameof(quotes));
                }

                // Create properly formatted JSON including both IDs
                var quotesData = quotes.Select(q => new
                {
                    SystemId = q.SystemId2.ToString(), // pass both ids as SystemId2. Dont change please 
                    SystemId2 = q.SystemId2.ToString(),
                    ItemNo = q.ItemNo,
                    Price = q.UnitCost,
                    PriceIncludesVAT = q.PriceIncludesVAT,
                    VatOption = q.VatOption,
                    VatPercentage = q.VatPercentage,
                    Quantity = q.Quantity
                }).ToList();

                // Serialize to JSON string
                var quotesJson = JsonSerializer.Serialize(quotesData);

                var requestBody = new SubmitMultipleQuotesBody
                {
                    vendorNo = vendorNo,
                    quotesJson = quotesJson
                };

                var request = new SubmitMultipleQuotes(requestBody);
                var response = await _soapClient.SubmitMultipleQuotesAsync(request);

                _logger.LogInformation("Successfully submitted {Count} quotes for vendor {VendorNo}", quotes.Count, vendorNo);
                return response.Body.return_value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting {Count} quotes for vendor {VendorNo}", quotes?.Count, vendorNo);
                throw new Exception($"Failed to submit quotes: {ex.Message}", ex);
            }
        }

        public async Task<List<Guid>> GetQuotedLineIdsAsync(string vendorNo)
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var endpoint = $"Company('{encodedCompanyName}')/RFQVendorQuotesAPI?$filter=prequalifiedSupplierNo eq '{vendorNo}'";

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<RfqVendorQuoteDto>>(endpoint);

                return response?.Value
                    .Select(q => Guid.TryParse(q.LineSystemId, out var guid) ? guid : Guid.Empty)
                    .Where(guid => guid != Guid.Empty)
                    .ToList() ?? new List<Guid>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quoted line IDs for vendor {VendorNo}", vendorNo);
                return new List<Guid>();
            }
        }

        public async Task<decimal> GetDefaultVatPercentageAsync()
        {
            try
            {
                var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
                var endpoint = $"Company('{encodedCompanyName}')/PurchasesPayablesSetup";

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<PurchasesPayablesSetupDto>>(endpoint);
                return response?.Value?.FirstOrDefault()?.DefaultVatPercentage ?? 16.00m; // Default fallback
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default VAT percentage");
                return 16.00m; // Default fallback
            }
        }

        public class PurchasesPayablesSetupDto
        {
            [JsonPropertyName("defaultVatPercentage")]
            public decimal DefaultVatPercentage { get; set; }
        }

        public class RfqVendorQuoteDto
        {
            [JsonPropertyName("lineSystemId")]
            public string LineSystemId { get; set; }

            [JsonPropertyName("vendorNo")]
            public string VendorNo { get; set; }
        }
    }
}