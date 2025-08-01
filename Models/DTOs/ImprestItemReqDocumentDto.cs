using System;
using System.Text.Json.Serialization;

namespace RecruitmentPortal.Models.DTOs
{
    public class ImprestItemReqDocumentDto
    {
        [JsonPropertyName("systemId")]
        public string SystemId { get; set; }

        [JsonPropertyName("requisitionNo")]
        public string RequisitionNo { get; set; }

        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [JsonPropertyName("fileExtension")]
        public string FileExtension { get; set; }

        [JsonPropertyName("documentType")]
        public string DocumentType { get; set; }

        [JsonPropertyName("securityLevel")]
        public string SecurityLevel { get; set; }

        [JsonPropertyName("isEncrypted")]
        public bool IsEncrypted { get; set; }

        [JsonPropertyName("uploadedBy")]
        public string UploadedBy { get; set; }

        [JsonPropertyName("uploadedDate")]
        public DateTime UploadedDate { get; set; }

        public Guid SystemIdGuid => Guid.TryParse(SystemId, out var guid) ? guid : Guid.Empty;
    }
}