using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RecruitmentPortal.Models.DTOs
{
    public class ImprestItemReqDto
    {
        [JsonPropertyName("@odata.context")]
        public string ODataContext { get; set; }

        [JsonPropertyName("systemId")]
        public string SystemId { get; set; }

        [JsonPropertyName("no")]
        public string No { get; set; }

        [JsonPropertyName("requisitionType")]
        public string RequisitionType { get; set; }

        [JsonPropertyName("requestorId")]
        public string RequestorId { get; set; }

        [JsonPropertyName("requestDate")]
        public DateTime RequestDate { get; set; }

        [JsonPropertyName("postingDate")]
        public DateTime PostingDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("documentCount")]
        public int DocumentCount { get; set; }

        [JsonPropertyName("documents")]
        public List<ImprestItemReqDocumentDto> Documents { get; set; } = new List<ImprestItemReqDocumentDto>();

        public Guid SystemIdGuid => Guid.TryParse(SystemId, out var guid) ? guid : Guid.Empty;
    }
}