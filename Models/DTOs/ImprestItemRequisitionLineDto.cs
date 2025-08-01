using System.Text.Json.Serialization;

namespace RecruitmentPortal.Models.DTOs
{
    public class ImprestItemRequisitionLineDto
    {
        [JsonPropertyName("systemId")]
        public string SystemId { get; set; }

        [JsonPropertyName("documentNo")]
        public string DocumentNo { get; set; }

        [JsonPropertyName("lineNo")]
        public int LineNo { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } // "Item" or "G/L Account"

        [JsonPropertyName("no")]
        public string No { get; set; } // Item No or G/L Account No

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("unitOfMeasure")]
        public string UnitOfMeasure { get; set; }

        [JsonPropertyName("unitCost")]
        public decimal UnitCost { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("locationCode")]
        public string LocationCode { get; set; }

        [JsonPropertyName("binCode")]
        public string BinCode { get; set; }

        [JsonPropertyName("jobNo")]
        public string JobNo { get; set; }

        [JsonPropertyName("jobTaskNo")]
        public string JobTaskNo { get; set; }

        [JsonPropertyName("shortcutDimension1Code")]
        public string ShortcutDimension1Code { get; set; } // Added this property

        [JsonPropertyName("shortcutDimension2Code")]
        public string ShortcutDimension2Code { get; set; } // Added this property

        public Guid SystemIdGuid => Guid.TryParse(SystemId, out var guid) ? guid : Guid.Empty;
    }
}