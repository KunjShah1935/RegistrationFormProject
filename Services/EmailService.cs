using System.Net;
using System.Net.Mail;
using RegistrationFormProject.Models;

namespace RegistrationFormProject.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(
            IConfiguration configuration)
        {
            _settings =
                configuration
                .GetSection("EmailSettings")
                .Get<EmailSettings>();
        }

        public async Task SendOtpEmailAsync(
            string toEmail,
            string otp)
        {
            var message =
                new MailMessage();

            message.From =
                new MailAddress(
                    _settings.Mail);

            message.To.Add(toEmail);

            message.Subject =
                "Password Reset OTP";

            message.Body =
$@"Hello,

Your OTP for password reset is:

{otp}

This OTP is valid for 5 minutes.

If you didn't request this, please ignore this email.";

            using var smtp =
                new SmtpClient(
                    _settings.Host,
                    _settings.Port);

            smtp.UseDefaultCredentials = false;
            smtp.Credentials =
                new NetworkCredential(
                    _settings.Mail,
                    _settings.Password);

            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.EnableSsl = true;

            await smtp.SendMailAsync(message);
        }
    }
}