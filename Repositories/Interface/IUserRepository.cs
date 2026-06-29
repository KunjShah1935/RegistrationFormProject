using RegistrationFormProject.Models;
using RegistrationFormProject.ViewModels;

namespace RegistrationFormProject.Repositories.Interfaces
{
    public interface IUserRepository
    {
        // User Management

        Task<UserMaster?> GetUserByIdAsync(int id);

        Task<int> AddUserAsync(UserMaster user);

        Task<int> DeleteUserAsync(int id);

        Task<List<UserListVM>> GetAllUsersAsync();

        Task<List<UserVerificationVM>>
            GetVerificationDashboardAsync();

        // Authentication / Forgot Password

        Task<UserMaster?> GetUserByUsernameAsync(
            string username);

        Task SaveOtpAsync(
            int userId,
            string otp,
            DateTime expiry,
            string method);

        Task<bool> ValidateOtpAsync(
            int userId,
            string otp);

        Task MarkOtpUsedAsync(
            int userId,
            string otp);

        Task UpdatePasswordAsync(
            int userId,
            string hashedPassword);
    }
}