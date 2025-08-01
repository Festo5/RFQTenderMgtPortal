using System.Text.Json.Serialization;

namespace RecruitmentPortal.Models.DTOs
{
    public class ImprestItemRequisitionHeaderDto
    {
        [JsonPropertyName("systemId")]
        public string SystemId { get; set; }

        [JsonPropertyName("no")]
        public string No { get; set; }

        [JsonPropertyName("reqTypeAPI")]
        public string ReqTypeAPI { get; set; }

        [JsonPropertyName("requisitionType")]
        public string RequisitionType { get; set; }

        [JsonPropertyName("requestorId")]
        public string RequestorId { get; set; }

        [JsonPropertyName("requestDate")]
        public DateTime RequestDate { get; set; }

        [JsonPropertyName("postingDate")]
        public DateTime PostingDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } // "Open", "Posted", "Cancelled"

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("shortcutDimension1Code")]
        public string DepartmentCode { get; set; }

        [JsonPropertyName("shortcutDimension2Code")]
        public string ProjectCode { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("documentCount")]
        public int DocumentCount { get; set; }
        public Guid SystemIdGuid => Guid.TryParse(SystemId, out var guid) ? guid : Guid.Empty;
    }
}