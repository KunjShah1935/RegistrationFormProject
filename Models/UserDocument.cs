using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegistrationFormProject.Models
{
    public class UserDocument
    {
        [Key]
        public int DocumentId { get; set; }

        public int UserId { get; set; }

        public string DocumentType { get; set; }

        public string FileName { get; set; }

        public string FilePath { get; set; }

        public string? CloudinaryUrl { get; set; }

        public string? CloudinaryPublicId { get; set; }

        public DateTime UploadedDate { get; set; }
        [ForeignKey("UserId")]
        public UserMaster UserMaster { get; set; }

        public bool IsVerified { get; set; } = false;

        public DateTime? VerifiedDate { get; set; }

        public int? VerifiedBy { get; set; }

        public bool NeedsReupload { get; set; } = false;

        public string? ReuploadReason { get; set; }
    }
}