using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RecruitmentPortal.Models.DTOs
{
    public class ItemDto
    {
        public string No { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public decimal Inventory { get; set; }
        public decimal UnitCost { get; set; }
        public string UnitOfMeasure { get; set; }
    }
}