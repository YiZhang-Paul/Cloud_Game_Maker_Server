using Amazon.S3;
using Core.Models.GameScenes;
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
        private ICloudStorageService CloudStorageService { get; set; }

        public CloudStorageController(IAmazonS3 s3, ICloudStorageService cloudStorageService)
        {
            S3 = s3;
            CloudStorageService = cloudStorageService;
        }

        [HttpGet]
        [Route("scenes")]
        public async Task<IEnumerable<Scene>> GetScenes()
        {
            var response = await S3.ListObjectsAsync(BucketName, "scenes").ConfigureAwait(false);

            return response.S3Objects.Select(_ => new Scene
            {
                Id = _.Key,
                Name = Regex.Replace(_.Key, $"^.*/|\\.json$", string.Empty)
            });
        }

        [HttpPost]
        [Route("scenes")]
        public async Task<string> AddScene([FromBody]Scene scene)
        {
            if (string.IsNullOrWhiteSpace(scene?.Name))
            {
                return null;
            }

            var key = $"scenes/{scene.Name}.json";
            var json = JsonSerializer.Serialize(scene);

            return await CloudStorageService.UploadFile(json, BucketName, key, "application/json").ConfigureAwait(false);
        }

        [HttpGet]
        [Route("sprites/{id}")]
        public async Task<IActionResult> GetSprite(string id)
        {
            var response = await S3.GetObjectAsync(BucketName, WebUtility.UrlDecode(id)).ConfigureAwait(false);

            return File(response.ResponseStream, "image/jpeg");
        }

        [HttpGet]
        [Route("sprites")]
        public async Task<IEnumerable<SpriteFile>> GetSprites()
        {
            var response = await S3.ListObjectsAsync(BucketName, "sprites").ConfigureAwait(false);

            return response.S3Objects.Select(_ => new SpriteFile
            {
                Id = _.Key,
                Name = Regex.Replace(_.Key, $"^.*/|\\.jpg$", string.Empty),
                Mime = "image/jpeg",
                Extension = "jpg",
                OriginalUrl = CloudStorageService.GetPreSignedURL(BucketName, _.Key, 2),
                ThumbnailUrl = CloudStorageService.GetThumbnailPreSignedURL(BucketName, _.Key, 2)
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
            await CloudStorageService.GenerateThumbnail(file, BucketName, key).ConfigureAwait(false);

            return await CloudStorageService.UploadFile(file, BucketName, key, sprite.Mime).ConfigureAwait(false);
        }

        [HttpPut]
        [Route("sprites/{originatedId}")]
        public async Task<string> UpdateSprite([FromForm]IFormFile file, [FromForm]string spriteJson, string originatedId)
        {
            if (!await DeleteSprite(originatedId).ConfigureAwait(false))
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
            _ = CloudStorageService.DeleteThumbnail(BucketName, key).ConfigureAwait(false);

            return await CloudStorageService.DeleteFile(BucketName, key).ConfigureAwait(false);
        }
    }
}
