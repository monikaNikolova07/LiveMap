using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using LiveMap.Core.Contracts;
using LiveMap.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.IO;

namespace LiveMap.Core.Services
{
    public class ImageService : IImageService
    {

        private readonly Cloudinary cloudinary;

        public ImageService(IOptions<CloudinarySettings> options)
        {
            var settings = options.Value;
            var account = new Account(
                settings.CloudName,
                settings.ApiKey,
                settings.ApiSecret
            );
            this.cloudinary = new Cloudinary(account);
        }

        public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile imageFile, string name, string folder)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                throw new ArgumentException("File is empty!");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Invalid file type. Allowed types are: .jpg, .jpeg, .png");
            }

            using var stream = imageFile.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(imageFile.FileName, stream),
                Folder = folder,
            };

            var uploadResult = await cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Cloudinary error: {uploadResult.Error.Message}");
            }

            return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
        }
    }
}