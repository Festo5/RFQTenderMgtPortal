using System.Collections.Generic;

namespace RecruitmentPortal.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int OpenRfqCount { get; set; }
        public int ClosedRfqCount { get; set; }
        public int AwardedRfqCount { get; set; }
        public int OpenTenderCount { get; set; }
        public int ClosedTenderCount { get; set; }
        public int AwardedTenderCount { get; set; }
        public int OpenImprestRequisitionCount { get; set; }
        public int OpenItemRequisitionCount { get; set; }
        public int CompletedRequisitionCount { get; set; }
        public int CancelledItemRequisitionCount { get; set; }
        public int ApprovedRequisitionCount { get; set; }
        public ColorSettingsViewModel ColorSettings { get; set; }
    }
}