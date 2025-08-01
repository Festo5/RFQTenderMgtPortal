namespace RecruitmentPortal.Models.DTOs
{
    public class QuoteSubmissionModel
    {
        public Guid SystemId { get; set; } = Guid.NewGuid(); // Auto-generated
        public string ItemNo { get; set; }
        public decimal UnitCost { get; set; }
        public bool PriceIncludesVAT { get; set; }
        public string VatOption { get; set; } = "Vatable";
        public decimal VatPercentage { get; set; } = 16.0m;
        public decimal Quantity { get; set; }
        public Guid SystemId2 { get; set; } // Passed from client
    }
}