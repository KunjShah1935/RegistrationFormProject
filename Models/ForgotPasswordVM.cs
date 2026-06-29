using System.ComponentModel.DataAnnotations;

namespace RegistrationFormProject.Models
{
    public class ForgotPasswordVM
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Method { get; set; }

        public string? OTP { get; set; }

        public string? NewPassword { get; set; }

        [Compare(
            "NewPassword",
            ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}