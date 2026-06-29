using System.ComponentModel.DataAnnotations;

namespace RegistrationFormProject.Models
{
    public class RoleMaster
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        public string RoleName { get; set; }
    }
}
