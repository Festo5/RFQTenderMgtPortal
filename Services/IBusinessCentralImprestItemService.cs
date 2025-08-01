using RecruitmentPortal.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecruitmentPortal.Services
{
    public interface IBusinessCentralImprestItemService
    {
        Task<List<ImprestItemRequisitionHeaderDto>> GetRequisitionHeadersAsync(string userId = null);
        Task<List<ImprestItemRequisitionLineDto>> GetRequisitionLinesAsync(string documentNo);
        Task<string> SubmitRequisitionAsync(
            string userId,
            string requisitionType,
            string description,
            List<ImprestItemRequisitionLineDto> lines);
        Task<string> EditRequisitionAsync(
            string userId,
            string documentNo,
            string postingDate,
            string description,
            string departmentCode,
            string projectCode,
            List<ImprestItemRequisitionLineDto> lines);
        Task<string> PostRequisitionAsync(string documentNo);
        Task<string> CancelRequisitionAsync(string documentNo);
        Task<List<Dimension1Dto>> GetDimension1ListAsync();
        Task<List<Dimension2Dto>> GetDimension2ListAsync();
        Task<List<ItemDto>> GetItemListAsync();
        Task<List<GLAccountDto>> GetGLAccountListAsync();

        // Document related methods
        Task<List<ImprestItemReqDocumentDto>> GetRequisitionDocumentsAsync(string documentNo);
        Task<bool> UploadRequisitionDocumentAsync(
            string requisitionNo,
            string fileName,
            byte[] fileContent,
            string documentType = "Other",
            string securityLevel = "Internal",
            bool isEncrypted = false,
            string uploadedBy = null);
        Task<bool> DeleteRequisitionDocumentAsync(string documentId);
    }
}