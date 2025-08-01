using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models.ViewModels.ImprestItem
{
    public class ImprestItemRequisitionLineCreateViewModel
    {
        [Required]
        [Display(Name = "Type")]
        public string Type { get; set; } // "Item" or "G/L Account"

        [Required]
        [Display(Name = "No.")]
        public string No { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Quantity")]
        public decimal Quantity { get; set; } = 1;

        [Display(Name = "Unit of Measure")]
        public string UnitOfMeasure { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Unit Cost")]
        public decimal UnitCost { get; set; }

        [Display(Name = "Location Code")]
        public string LocationCode { get; set; }

        [Display(Name = "Bin Code")]
        public string BinCode { get; set; }

        [Display(Name = "Job No.")]
        public string JobNo { get; set; }

        [Display(Name = "Job Task No.")]
        public string JobTaskNo { get; set; }
    }
}