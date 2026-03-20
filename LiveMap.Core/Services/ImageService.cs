using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using LiveMap.Core.Contracts;
using LiveMap.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            var allowedTypes = new[] { "image/jpg", "image/jpeg", "image/png" };

            if (!allowedTypes.Contains(imageFile.ContentType))

            {

                throw new ArgumentException("Invalid file type");

            }

            var uniqieName = $"{Guid.NewGuid()}_{name}";

            using var stream = imageFile.OpenReadStream();

            var uploadParams = new ImageUploadParams()

            {

                File = new FileDescription(name, stream),

                Folder = folder,

            };

            var uploadResult = await cloudinary.UploadAsync(uploadParams);

            return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);

        }
    }
}