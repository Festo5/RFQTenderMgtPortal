using RecruitmentPortal.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecruitmentPortal.Services
{
    public interface IBusinessCentralTenderService
    {
        Task<List<TenderDto>> GetPublishedTendersAsync(string vendorNo = null);
        Task<TenderDto> GetTenderDocumentAsync(string systemId);
        Task<TenderDto> GetTenderByIdAsync(Guid id);
        Task<(int Open, int Closed, int Awarded)> GetTenderCountsByStatusAsync(string vendorNo = null);
        Task<List<TenderAttachmentDto>> GetTenderAttachmentsAsync(Guid tenderId);

        Task<bool> UploadTenderDocumentAsync(
            string tenderNo,
            string fileName,
            byte[] fileContent,
            string documentType = "Other",
            string securityLevel = "Internal",
            bool isEncrypted = false,
            string vendorNo = null);

        Task<bool> DeleteTenderAttachmentAsync(string attachmentId);
        Task<List<TenderDocumentDto>> GetTenderDocumentsAsync(Guid tenderId, string vendorNo = null);
        Task<bool> DeleteTenderDocumentAsync(string documentId);
    }
}