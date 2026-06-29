namespace RegistrationFormProject.ViewModels
{
    public class DocumentVerificationVM
    {
        public int DocumentId { get; set; }

        public string FullName { get; set; }

        public string DocumentType { get; set; }

        public string FileName { get; set; }

        public bool IsVerified { get; set; }

        public DateTime UploadedDate { get; set; }

        public bool NeedsReupload { get; set; }

        public string? ReuploadReason { get; set; }
    }
}