using System;
using System.ComponentModel.DataAnnotations;

namespace RegistrationFormProject.Models
{
    public class ActivityLog
    {
        [Key]
        public int ActivityLogId { get; set; }

        public int? UserId { get; set; }

        [StringLength(100)]
        public string? UserName { get; set; }

        [Required]
        [StringLength(500)]
        public string Activity { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        public DateTime LoggedDate { get; set; } = DateTime.Now;
    }
}
