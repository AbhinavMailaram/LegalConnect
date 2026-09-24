using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using LegalConnect.Web.Models.Entities;

namespace LegalConnect.Web.Models.ViewModels
{
    public class MessageThreadViewModel
    {
        public int? AppointmentId { get; set; }
        public string OtherUserId { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public string OtherUserRole { get; set; } = string.Empty;
        public string CurrentUserId { get; set; } = string.Empty;
        public Appointment? Appointment { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();

        [Required(ErrorMessage = "Message text cannot be empty.")]
        [StringLength(2000, ErrorMessage = "Message cannot exceed 2000 characters.")]
        public string NewMessageText { get; set; } = string.Empty;
    }

    public class DocumentUploadViewModel
    {
        public int? AppointmentId { get; set; }

        [Required(ErrorMessage = "Please select a file to upload.")]
        public IFormFile File { get; set; } = null!;

        public List<Document> ExistingDocuments { get; set; } = new List<Document>();
    }
}
