using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RecruitmentPortal.Models.DTOs
{
    public class GLAccountDto
    {
        public string No { get; set; }
        public string Name { get; set; }
        public string AccountType { get; set; }
    }
}