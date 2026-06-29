using System;
using System.ComponentModel.DataAnnotations;

namespace RegistrationFormProject.Models
{
    public class ErrorLog
    {
        [Key]
        public int ErrorLogId { get; set; }

        public int? UserId { get; set; }

        [StringLength(100)]
        public string? UserName { get; set; }

        [Required]
        [StringLength(100)]
        public string ControllerName { get; set; }

        [Required]
        [StringLength(100)]
        public string ActionName { get; set; }

        [Required]
        public string ErrorMessage { get; set; }

        public string? StackTrace { get; set; }

        public DateTime LoggedDate { get; set; } = DateTime.Now;
    }
}
