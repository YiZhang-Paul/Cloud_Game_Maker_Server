using Microsoft.AspNetCore.Http;
using System.Drawing;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface IImageService
    {
        Task<Image> GetThumbnailImage(int width, int height, IFormFile file);
    }
}
