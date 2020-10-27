using Amazon.S3;
using Core.Models.GameSprites;
using Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/cloud-storage")]
    public class CloudStorageController : ControllerBase
    {
        private const string BucketName = "cloud-game-maker";
        private IAmazonS3 S3 { get; set; }
        private IS3Service S3Service { get; set; }

        public CloudStorageController(IAmazonS3 s3, IS3Service s3Service)
        {
            S3 = s3;
            S3Service = s3Service;
        }

        [HttpGet]
        [Route("sprites")]
        public async Task<IEnumerable<SpriteFile>> GetSprites()
        {
            var response = await S3.ListObjectsAsync(BucketName, "sprites").ConfigureAwait(false);

            return response.S3Objects.Select(_ => new SpriteFile
            {
                Id = _.Key,
                Name = Regex.Replace(_.Key, $"^.*/|\\.(jpg|png)$", string.Empty),
                Mime = Regex.IsMatch(_.Key, "\\.png$") ? "image/png" : "image/jpeg",
                Extension = Regex.IsMatch(_.Key, "\\.png$") ? "png" : "jpg",
                OriginalUrl = S3Service.GetPreSignedURL(BucketName, _.Key, 2),
                ThumbnailUrl = S3Service.GetThumbnailPreSignedURL(BucketName, _.Key, 2)
            });
        }

        [HttpPost]
        [Route("sprites")]
        public async Task<string> AddSprite([FromForm]IFormFile file, [FromForm]string spriteJson)
        {
            if (file == null || spriteJson == null)
            {
                return null;
            }

            var option = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var sprite = JsonSerializer.Deserialize<SpriteFile>(spriteJson, option);
            var key = $"sprites/{sprite.Name}.{sprite.Extension}";
            await S3Service.GenerateThumbnail(file, BucketName, key).ConfigureAwait(false);

            return await S3Service.UploadFile(file, BucketName, key, sprite.Mime).ConfigureAwait(false);
        }

        [HttpPut]
        [Route("sprites/{originatedId}")]
        public async Task<string> UpdateSprite([FromForm]IFormFile file, [FromForm]string spriteJson, string originatedId)
        {
            if (!await DeleteSprite(WebUtility.UrlDecode(originatedId)).ConfigureAwait(false))
            {
                return null;
            }

            return await AddSprite(file, spriteJson).ConfigureAwait(false);
        }

        [HttpDelete]
        [Route("sprites/{id}")]
        public async Task<bool> DeleteSprite(string id)
        {
            var key = WebUtility.UrlDecode(id);
            _ = S3Service.DeleteThumbnail(BucketName, key).ConfigureAwait(false);

            return await S3Service.DeleteFile(BucketName, key).ConfigureAwait(false);
        }
    }
}
