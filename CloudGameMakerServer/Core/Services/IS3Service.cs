using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface IS3Service
    {
        Task<string> UploadFile(IFormFile file, string bucket, string key, string mime);
        Task<string> GenerateThumbnail(IFormFile file, string bucket, string key, int width = 100, int height = 100);
    }
}
