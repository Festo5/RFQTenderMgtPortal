using RecruitmentPortal.Models.DTOs;
using System.Text.Json.Serialization;

public interface IColorSettingsService
{
    Task<ColorSettingsViewModel> GetColorSettingsAsync();
}

public class ColorSettingsService : IColorSettingsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ColorSettingsService> _logger;

    public ColorSettingsService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<ColorSettingsService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("BusinessCentral");
        _config = config;
        _logger = logger;
    }

    public async Task<ColorSettingsViewModel> GetColorSettingsAsync()
    {
        try
        {
            var encodedCompanyName = Uri.EscapeDataString(_config["BusinessCentral:OData:Company"]);
            var endpoint = $"Company('{encodedCompanyName}')/RFQTenderSetupAPI";

            var response = await _httpClient.GetFromJsonAsync<ODataResponse<RfqTenderSetupDto>>(endpoint);
            var setup = response?.Value?.FirstOrDefault();

            return new ColorSettingsViewModel
            {
                PrimaryColor = setup?.PrimaryColor ?? "#0d6efd",
                SecondaryColor = setup?.SecondaryColor ?? "#6c757d",
                Tertiary1Color = setup?.Tertiary1Color ?? "#198754", // Success color
                Tertiary2Color = setup?.Tertiary2Color ?? "#ffc107", // Warning color
                Tertiary3Color = setup?.Tertiary3Color ?? "#0dcaf0"  // Accent color
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting color settings");
            return new ColorSettingsViewModel(); // Returns defaults
        }
    }

    public class RfqTenderSetupDto
    {
        [JsonPropertyName("primaryColor")]
        public string PrimaryColor { get; set; }

        [JsonPropertyName("secondaryColor")]
        public string SecondaryColor { get; set; }

        [JsonPropertyName("tertiary1Color")]
        public string Tertiary1Color { get; set; }

        [JsonPropertyName("tertiary2Color")]
        public string Tertiary2Color { get; set; }

        [JsonPropertyName("tertiary3Color")]
        public string Tertiary3Color { get; set; }
    }
}