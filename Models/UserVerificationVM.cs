namespace RegistrationFormProject.Models
{
    public class UserVerificationVM
    {
        public int UserId { get; set; }

        public string FullName { get; set; }

        public int TotalDocuments { get; set; }

        public int VerifiedDocuments { get; set; }

        public int ReuploadDocuments { get; set; }

        public string VerificationStatus { get; set; }
    }
}