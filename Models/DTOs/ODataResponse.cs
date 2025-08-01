using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RecruitmentPortal.Models.DTOs
{
    public class ODataResponse<T>
    {
        [JsonPropertyName("value")]
        public List<T> Value { get; set; }
    }
}