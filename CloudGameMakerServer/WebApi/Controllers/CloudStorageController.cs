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
        private ICloudStorageService CloudStorageService { get; set; }

        public CloudStorageController(ICloudStorageService cloudStorageService)
        {
            CloudStorageService = cloudStorageService;
        }

        [HttpGet]
        [Route("scenes")]
        public async Task<IEnumerable<Scene>> GetScenes()
        {
            var metas = await CloudStorageService.GetMetas(BucketName, "scenes").ConfigureAwait(false);

            return metas.Select(_ => new Scene
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

            scene.Id = $"scenes/{scene.Name}.json";
            var option = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(scene, option);

            return await CloudStorageService.UploadFile(json, BucketName, scene.Id, "application/json").ConfigureAwait(false);
        }

        [HttpPut]
        [Route("scenes")]
        public async Task<string> UpdateScene([FromBody]Scene scene)
        {
            if (!await DeleteScene(scene.Id).ConfigureAwait(false))
            {
                return null;
            }

            return await AddScene(scene).ConfigureAwait(false);
        }

        [HttpDelete]
        [Route("scenes/{id}")]
        public async Task<bool> DeleteScene(string id)
        {
            return await CloudStorageService.DeleteFile(BucketName, WebUtility.UrlDecode(id)).ConfigureAwait(false);
        }

        [HttpGet]
        [Route("sprites/{id}")]
        public async Task<IActionResult> GetSprite(string id)
        {
            var key = WebUtility.UrlDecode(id);
            var file = await CloudStorageService.GetFile(BucketName, key).ConfigureAwait(false);

            return File(file, "image/jpeg");
        }

        [HttpGet]
        [Route("sprites")]
        public async Task<IEnumerable<SpriteFile>> GetSprites()
        {
            var metas = await CloudStorageService.GetMetas(BucketName, "sprites").ConfigureAwait(false);

            return metas.Select(_ => new SpriteFile
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
