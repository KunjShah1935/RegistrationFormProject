namespace RegistrationFormProject.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(
            string toEmail,
            string otp);
    }
}