using System.Text.Json.Serialization;

namespace RecruitmentPortal.Models.DTOs
{
    public class RfqPublishedLineDto
    {
        [JsonPropertyName("systemId")]
        public string SystemId { get; set; }

        [JsonPropertyName("worksheetTemplateName")]
        public string WorksheetTemplateName { get; set; }

        [JsonPropertyName("journalBatchName")]
        public string JournalBatchName { get; set; }

        [JsonPropertyName("lineNo")]
        public int LineNo { get; set; }

        [JsonPropertyName("no")]
        public string No { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("directUnitCost")]
        public decimal DirectUnitCost { get; set; }

        [JsonPropertyName("categoryCode")]
        public string CategoryCode { get; set; }

        [JsonPropertyName("published")]
        public bool Published { get; set; }

        [JsonPropertyName("approvedForRFQ")]
        public bool ApprovedForRFQ { get; set; }

        [JsonPropertyName("rfqExpirationDate")]
        public DateTime? RfqExpirationDate { get; set; }

        [JsonPropertyName("rfqPublishDate")]
        public DateTime? RfqPublishDate { get; set; }

        [JsonPropertyName("daysRemaining")]
        public int DaysRemaining { get; set; }

        [JsonPropertyName("isAlreadyQuoted")]
        public bool IsAlreadyQuoted { get; set; }

        [JsonPropertyName("priceIncludesVAT")]
        public bool PriceIncludesVAT { get; set; }

        [JsonPropertyName("additionalNotes")]
        public string AdditionalNotes { get; set; }

        [JsonPropertyName("specifications")]
        public string Specifications { get; set; }

        [JsonPropertyName("rfqStatus")]
        public string RfqStatus { get; set; }

        [JsonPropertyName("vatOption")]
        public string VatOption { get; set; } = "Vatable";

        [JsonPropertyName("vatPercentage")]
        public decimal VatPercentage { get; set; } = 16.00m;

        [JsonPropertyName("awardedtoVendorNo")]
        public string AwardedToVendorNo { get; set; }

        [JsonPropertyName("rfqDateAwarded")]
        public DateTime? AwardDate { get; set; }

        // Helper property to get SystemId as Guid
        public Guid SystemIdGuid => Guid.TryParse(SystemId, out var guid) ? guid : Guid.Empty;
    }
}