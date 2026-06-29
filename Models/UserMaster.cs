using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace RegistrationFormProject.Models
{
    public class UserMaster
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        [Required]
        [NotMapped]
        [Compare("Password", ErrorMessage = "Password and Confirm Password must match")]
        public string ConfirmPassword { get; set; }

        [Required]
        [StringLength(50)]
        public string EmailID { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]{10}$")]
        public string MobileNo { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DOB { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public int RoleId { get; set; }

        [ForeignKey("RoleId")]
        public RoleMaster?RoleMaster { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsApproved { get; set; } = true;

        public bool IsSuspended { get; set; } = false;

        public int FailedLoginAttempts { get; set; } = 0;

        public bool IsProfileVerified { get; set; } = false;

        public bool IsSuperAdmin { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }
}
