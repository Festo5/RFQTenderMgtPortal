// Models/DTOs/JobDetailsDto.cs
using System.Text.Json.Serialization;

namespace RecruitmentPortal.Models.DTOs
{
    public class JobDetailsDto
    {
        [JsonPropertyName("id")]  // Changed to match BC OData response
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("location")]
        public string Location { get; set; }

        [JsonPropertyName("jobtype")]
        public string JobType { get; set; }

        // Add other properties as needed from your BC OData endpoint
    }
}