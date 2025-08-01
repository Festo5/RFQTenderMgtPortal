using System.Collections.Generic;
using RecruitmentPortal.Models.DTOs;

namespace RecruitmentPortal.Models.ViewModels
{
    public class TenderViewModel
    {
        public List<TenderDto> Tenders { get; set; } = new List<TenderDto>();
        public string CurrentVendorNo { get; set; }
        public int OpenTenderCount { get; set; }
        public int ClosedTenderCount { get; set; }
        public int AwardedTenderCount { get; set; }
    }
}