
using System.Text.Json.Serialization;

namespace RecruitmentPortal.Models.DTOs
{
    public class RfqVendorCategoryDto
    {
        [JsonPropertyName("systemId")]
        public string SystemId { get; set; }

        [JsonPropertyName("vendorNo")]
        public string VendorNo { get; set; }

        [JsonPropertyName("categoryCode")]
        public string CategoryCode { get; set; }
    }
}