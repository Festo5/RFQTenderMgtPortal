// ImprestItemRequisitionCreateViewModel.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models.ViewModels.ImprestItem
{
    public class ImprestItemRequisitionCreateViewModel
    {
        [Required]
        [Display(Name = "Requisition Type")]
        public string RequisitionType { get; set; } // "Imprest" or "Item"

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Department Code")]
        public string DepartmentCode { get; set; }

        [Display(Name = "Project Code")]
        public string ProjectCode { get; set; }

        [Display(Name = "Posting Date")]
        public DateTime PostingDate { get; set; } = DateTime.Today;

        public List<ImprestItemRequisitionLineCreateViewModel> Lines { get; set; } = new();
    }
}