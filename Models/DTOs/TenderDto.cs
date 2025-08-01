using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RecruitmentPortal.Models.DTOs
{
    public class TenderDto
    {
        [JsonPropertyName("@odata.context")]
        public string ODataContext { get; set; }

        [JsonPropertyName("systemId")]
        public string SystemId { get; set; }

        [JsonPropertyName("tenderNo")]
        public string TenderNo { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("categoryCode")]
        public string CategoryCode { get; set; }

        [JsonPropertyName("publishDate")]
        public DateTime PublishDate { get; set; }

        [JsonPropertyName("closingDate")]
        public DateTime ClosingDate { get; set; }

        [JsonPropertyName("daysRemaining")]
        public int DaysRemaining { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("approved")]
        public bool Approved { get; set; }

        [JsonPropertyName("published")]
        public bool Published { get; set; }

        [JsonPropertyName("securityLevel")]
        public string SecurityLevel { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("TenderDocumentsAPIattachments")]
        public List<TenderAttachmentDto> Attachments { get; set; } = new List<TenderAttachmentDto>();

        public Guid SystemIdGuid => Guid.TryParse(SystemId, out var guid) ? guid : Guid.Empty;
    }

    public class TenderAttachmentDto
    {
        [JsonPropertyName("systemId")]
        public string SystemId { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [JsonPropertyName("fileExtension")]
        public string FileExtension { get; set; }

        [JsonPropertyName("documentType")]
        public string DocumentType { get; set; }

        [JsonPropertyName("attachedDate")]
        public DateTime AttachedDate { get; set; }

        [JsonPropertyName("attachedBy")]
        public string AttachedBy { get; set; }

        [JsonPropertyName("fileContent")]
        public string FileContent { get; set; }
    }
}