using Core.Models.Configurations;
using Core.Models.GameSprites;
using Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/v1/cloud-storage")]
    public class CloudStorageController : ControllerBase
    {
        private S3Configuration S3Configuration { get; set; }
        private ICloudStorageService CloudStorageService { get; set; }

        public CloudStorageController
        (
            IOptions<S3Configuration> s3Configuration,
            ICloudStorageService cloudStorageService
        )
        {
            S3Configuration = s3Configuration.Value;
            CloudStorageService = cloudStorageService;
        }

        [HttpGet]
        [Route("sprites/{id}")]
        public async Task<IActionResult> GetSprite(string id)
        {
            var key = WebUtility.UrlDecode(id);
            var file = await CloudStorageService.GetFile(S3Configuration.BucketName, key).ConfigureAwait(false);

            return File(file, "image/jpeg");
        }

        [HttpGet]
        [Route("sprites")]
        public async Task<IEnumerable<Sprite>> GetSprites()
        {
            var (bucketName, urlTimeAlive) = S3Configuration;
            var metas = await CloudStorageService.GetMetas(bucketName, "sprites").ConfigureAwait(false);

            return metas.Select(_ => new Sprite
            {
                Id = _.Key,
                Name = Regex.Replace(_.Key, $"^.*/|\\.jpg$", string.Empty),
                Mime = "image/jpeg",
                Extension = "jpg",
                OriginalUrl = CloudStorageService.GetPreSignedUrl(bucketName, _.Key, urlTimeAlive),
                ThumbnailUrl = CloudStorageService.GetThumbnailPreSignedUrl(bucketName, _.Key, urlTimeAlive)
            });
        }

        [HttpPost]
        [Route("sprites")]
        public async Task<Sprite> AddSprite([FromForm]IFormFile file, [FromForm]string spriteJson)
        {
            if (file == null || spriteJson == null)
            {
                return null;
            }

            var (bucketName, urlTimeAlive) = S3Configuration;
            var option = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var sprite = JsonSerializer.Deserialize<Sprite>(spriteJson, option);
            var key = $"sprites/{sprite.Name}.{sprite.Extension}";
            await CloudStorageService.GenerateThumbnail(file, bucketName, key).ConfigureAwait(false);
            sprite.Id = await CloudStorageService.UploadFile(file, bucketName, key, sprite.Mime).ConfigureAwait(false);

            if (sprite.Id == null)
            {
                return null;
            }

            sprite.OriginalUrl = CloudStorageService.GetPreSignedUrl(bucketName, sprite.Id, urlTimeAlive);
            sprite.ThumbnailUrl = CloudStorageService.GetThumbnailPreSignedUrl(bucketName, sprite.Id, urlTimeAlive);

            return sprite;
        }

        [HttpPut]
        [Route("sprites/{originatedId}")]
        public async Task<Sprite> UpdateSprite([FromForm]IFormFile file, [FromForm]string spriteJson, string originatedId)
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
            _ = CloudStorageService.DeleteThumbnail(S3Configuration.BucketName, key).ConfigureAwait(false);

            return await CloudStorageService.DeleteFile(S3Configuration.BucketName, key).ConfigureAwait(false);
        }
    }
}
