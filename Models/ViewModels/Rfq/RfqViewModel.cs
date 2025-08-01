using System;
using System.Collections.Generic;
using RecruitmentPortal.Models.DTOs;

namespace RecruitmentPortal.Models.ViewModels
{
    public class RfqViewModel
    {
        public List<RfqPublishedLineDto> OpenLines { get; set; } = new List<RfqPublishedLineDto>();
        public List<RfqPublishedLineDto> AwardedLines { get; set; } = new List<RfqPublishedLineDto>();
        public List<RfqVendorCategoryDto> VendorCategories { get; set; } = new List<RfqVendorCategoryDto>();
        public string CurrentVendorNo { get; set; }
        public string CurrentFilter { get; set; }
        public HashSet<Guid> QuotedLineIds { get; set; } = new HashSet<Guid>();
        public decimal DefaultVatPercentage { get; set; }
        public ColorSettingsViewModel ColorSettings { get; set; } = new();
    }
}