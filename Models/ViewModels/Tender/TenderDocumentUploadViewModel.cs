using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RecruitmentPortal.Models.ViewModels
{
    public class TenderDocumentUploadViewModel
    {
        public Guid TenderId { get; set; }
        public string TenderNo { get; set; }
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
        public string VendorNo { get; set; }

        public List<TenderDocumentDto> ExistingDocuments { get; set; } = new List<TenderDocumentDto>();
    }
}