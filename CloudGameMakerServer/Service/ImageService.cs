using Core.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Service
{
    public class ImageService : IImageService
    {
        public async Task<Image> GetThumbnailImage(int width, int height, IFormFile file)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream).ConfigureAwait(false);
            var image = Image.FromStream(stream);

            return image.GetThumbnailImage(width, height, () => false, IntPtr.Zero);
        }
    }
}
