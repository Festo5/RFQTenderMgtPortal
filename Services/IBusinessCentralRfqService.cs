using RecruitmentPortal.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecruitmentPortal.Services
{
    public interface IBusinessCentralRfqService
    {
        Task<List<RfqPublishedLineDto>> GetPublishedRfqLinesAsync(string vendorNo = null);
        Task<List<RfqVendorCategoryDto>> GetRfqVendorCategoriesAsync();
        Task<List<RfqVendorCategoryDto>> GetRfqVendorCategoriesByVendorAsync(string vendorNo);
        Task<string> SubmitQuoteAsync(string vendorNo, string itemNo, decimal unitCost, bool priceIncludesVAT, string vatOption, decimal vatPercentage, decimal quantity, Guid systemId2);
        Task<string> SubmitMultipleQuotesAsync(string vendorNo, List<QuoteSubmissionModel> quotes);
        Task<List<Guid>> GetQuotedLineIdsAsync(string vendorNo);
        Task<decimal> GetDefaultVatPercentageAsync();
    }
}