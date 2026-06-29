using System.ComponentModel.DataAnnotations;

namespace RegistrationFormProject.Models
{
    public class PasswordResetOtp
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public string OTP { get; set; }

        public DateTime ExpiryTime { get; set; }

        public bool IsUsed { get; set; }

        public string Method { get; set; }
    }
}