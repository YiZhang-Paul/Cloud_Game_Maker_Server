using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface ICloudStorageService
    {
        string GetPreSignedUrl(string bucket, string key, double hours);
        string GetThumbnailPreSignedUrl(string bucket, string key, double hours);
        Task<IEnumerable<S3Object>> GetMetas(string bucket, string folder);
        Task<Stream> GetFile(string bucket, string key);
        Task<string> UploadFile(IFormFile file, string bucket, string key, string mime);
        Task<string> UploadFile(string content, string bucket, string key, string mime);
        Task<string> GenerateThumbnail(IFormFile file, string bucket, string key, int width = 100, int height = 100);
        Task<bool> DeleteThumbnail(string bucket, string key);
        Task<bool> DeleteFile(string bucket, string key, bool ensureDelete = true);
    }
}
