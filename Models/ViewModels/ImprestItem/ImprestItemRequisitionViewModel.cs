// ImprestItemRequisitionViewModel.cs
using RecruitmentPortal.Models.DTOs;
using RecruitmentPortal.Models.DTOs.ImprestItem;
using RecruitmentPortal.Models.ViewModels.ImprestItem;
using System.Collections.Generic;

namespace RecruitmentPortal.Models.ViewModels.ImprestItem
{
    public class ImprestItemRequisitionViewModel
    {
        public List<ImprestItemRequisitionHeaderDto> OpenRequisitions { get; set; } = new();
        public List<ImprestItemRequisitionHeaderDto> PostedRequisitions { get; set; } = new();
        public List<ImprestItemRequisitionHeaderDto> CancelledRequisitions { get; set; } = new();
        public List<ImprestItemRequisitionHeaderDto> ApprovedRequisitions { get; set; } = new();
        public List<ImprestItemRequisitionLineDto> RequisitionLines { get; set; } = new();
        public string CurrentUserId { get; set; }
        public string CurrentFilter { get; set; }
        public ImprestItemRequisitionSetupDto Setup { get; set; }
        public dynamic ColorSettings { get; set; }
        public ImprestItemRequisitionCreateViewModel CreateModel { get; set; } = new ImprestItemRequisitionCreateViewModel();

        public List<Dimension1Dto> Dimension1List { get; set; } = new();
        public List<Dimension2Dto> Dimension2List { get; set; } = new();
        public List<ItemDto> ItemList { get; set; } = new();
        public List<GLAccountDto> GLAccountList { get; set; } = new();
    }
}