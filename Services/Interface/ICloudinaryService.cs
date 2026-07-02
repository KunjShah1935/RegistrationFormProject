using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace RegistrationFormProject.Services.Interface
{
    public interface ICloudinaryService
    {
        Task<(string SecureUrl, string PublicId)> UploadPdfAsync(IFormFile file);
        Task<bool> DeletePdfAsync(string publicId);
    }
}
