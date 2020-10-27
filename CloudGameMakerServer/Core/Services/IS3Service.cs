using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface IS3Service
    {
        string GetPreSignedURL(string bucket, string key, double hours);
        string GetThumbnailPreSignedURL(string bucket, string key, double hours);
        Task<string> UploadFile(IFormFile file, string bucket, string key, string mime);
        Task<string> GenerateThumbnail(IFormFile file, string bucket, string key, int width = 100, int height = 100);
        Task<bool> DeleteThumbnail(string bucket, string key);
        Task<bool> DeleteFile(string bucket, string key, bool ensureDelete = true);
    }
}
