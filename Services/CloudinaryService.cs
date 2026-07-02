using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RegistrationFormProject.Models;
using RegistrationFormProject.Services.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RegistrationFormProject.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> config)
        {
            var acc = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<(string SecureUrl, string PublicId)> UploadPdfAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (string.Empty, string.Empty);

            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "kyc_documents"
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
                }

                return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
            }
        }

        public async Task<bool> DeletePdfAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                return false;

            var deleteParams = new DelResParams
            {
                PublicIds = new List<string> { publicId },
                ResourceType = ResourceType.Raw
            };

            var deleteResult = await _cloudinary.DeleteResourcesAsync(deleteParams);
            return deleteResult.Deleted != null && deleteResult.Deleted.ContainsKey(publicId);
        }
    }
}
