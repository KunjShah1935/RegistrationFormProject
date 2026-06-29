namespace RegistrationFormProject.Models
{
    public class UserListVM
    {
        public int UserId { get; set; }

        public string FullName { get; set; }

        public string UserName { get; set; }

        public string EmailID { get; set; }

        public string MobileNo { get; set; }

        public DateTime DOB { get; set; }

        public string RoleName { get; set; }

        public DateTime CreatedDate { get; set; }
        
        public bool IsActive { get; set; }
        
        public bool IsSuperAdmin { get; set; }
    }
}