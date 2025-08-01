using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using RecruitmentPortal.Models.DTOs;  // Add this using directive

namespace RecruitmentPortal.Models.ViewModels.ImprestItem
{
    public class ImprestItemReqDocumentUploadViewModel
    {
        public string RequisitionNo { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }

        [Required(ErrorMessage = "Please select a file")]
        [Display(Name = "Document File")]
        public IFormFile DocumentFile { get; set; }

        [Display(Name = "Document Type")]
        public string DocumentType { get; set; } = "Other";

        [Display(Name = "Security Level")]
        public string SecurityLevel { get; set; } = "Internal";

        [Display(Name = "Encrypt Document")]
        public bool IsEncrypted { get; set; } = false;

        public List<ImprestItemReqDocumentDto> ExistingDocuments { get; set; } = new List<ImprestItemReqDocumentDto>();
    }
}