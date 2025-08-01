using System.Text.Json.Serialization;

namespace RecruitmentPortal.Models.DTOs.ImprestItem
{
    public class ImprestItemRequisitionSetupDto
    {
        [JsonPropertyName("requisitionNos")]
        public string RequisitionNos { get; set; }

        [JsonPropertyName("postedRequisitionNos")]
        public string PostedRequisitionNos { get; set; }

        [JsonPropertyName("itemJournalTemplate")]
        public string ItemJournalTemplate { get; set; }

        [JsonPropertyName("itemJournalBatch")]
        public string ItemJournalBatch { get; set; }

        [JsonPropertyName("genJournalTemplate")]
        public string GenJournalTemplate { get; set; }

        [JsonPropertyName("genJournalBatch")]
        public string GenJournalBatch { get; set; }
    }
}