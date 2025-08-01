using System.Text.Json.Serialization;

public class TenderDocumentDto
{
    [JsonPropertyName("systemId")]
    public string SystemId { get; set; }

    [JsonPropertyName("tenderNo")]
    public string TenderNo { get; set; }

    [JsonPropertyName("lineNo")]
    public int LineNo { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("fileName")]
    public string FileName { get; set; }

    [JsonPropertyName("fileExtension")]
    public string FileExtension { get; set; }

    [JsonPropertyName("fileSize")]
    public int FileSize { get; set; }

    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; }

    [JsonPropertyName("securityLevel")]
    public string SecurityLevel { get; set; }

    [JsonPropertyName("uploadedBy")]
    public string UploadedBy { get; set; }

    [JsonPropertyName("uploadedDate")]
    public DateTime UploadedDate { get; set; }

    [JsonPropertyName("isEncrypted")]
    public bool IsEncrypted { get; set; }

    public Guid SystemIdGuid => Guid.TryParse(SystemId, out var guid) ? guid : Guid.Empty;
}