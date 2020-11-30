using Core.Models.Configurations;
using Core.Models.GameScenes;
using Core.Models.GameSprites;
using Core.Services;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
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
        private S3Configuration S3Configuration { get; set; }
        private SceneDescriptorRepository SceneDescriptorRepository { get; set; }
        private ICloudStorageService CloudStorageService { get; set; }
        private IGameSceneService GameSceneService { get; set; }

        public CloudStorageController
        (
            IOptions<S3Configuration> s3Configuration,
            SceneDescriptorRepository sceneDescriptorRepository,
            ICloudStorageService cloudStorageService,
            IGameSceneService gameSceneService
        )
        {
            S3Configuration = s3Configuration.Value;
            SceneDescriptorRepository = sceneDescriptorRepository;
            CloudStorageService = cloudStorageService;
            GameSceneService = gameSceneService;
        }

        [HttpGet]
        [Route("scenes")]
        public async Task<IEnumerable<SceneDescriptor>> GetSceneDescriptors([FromQuery]int limit = 0)
        {
            return await SceneDescriptorRepository.Get(limit).ConfigureAwait(false);
        }

        [HttpGet]
        [Route("scenes/{id}")]
        public async Task<Scene> GetScene(string id)
        {
            return await GameSceneService.GetScene(id).ConfigureAwait(false);
        }

        [HttpPost]
        [Route("scenes")]
        public async Task<SceneDescriptor> AddScene([FromBody]Scene scene)
        {
            var key = await UploadScene(scene, $"scenes/{Guid.NewGuid()}.json").ConfigureAwait(false);

            if (key == null)
            {
                return null;
            }

            var descriptor = new SceneDescriptor { StorageKey = key, Name = scene.Name };
            await SceneDescriptorRepository.Add(descriptor);

            return descriptor.Id == null ? null : descriptor;
        }

        [HttpPut]
        [Route("scenes")]
        public async Task<bool> UpdateScene([FromBody]Scene scene)
        {
            var descriptor = await SceneDescriptorRepository.GetByStorageKey(scene.StorageKey).ConfigureAwait(false);

            if (descriptor == null)
            {
                return false;
            }

            var key = await UploadScene(scene, scene.StorageKey).ConfigureAwait(false);

            if (key != null)
            {
                descriptor.Name = scene.Name;
                await SceneDescriptorRepository.Replace(descriptor).ConfigureAwait(false);
            }

            return key != null;
        }

        private async Task<string> UploadScene(Scene scene, string key)
        {
            if (string.IsNullOrWhiteSpace(scene?.Name))
            {
                return null;
            }

            var option = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(scene, option);

            return await CloudStorageService.UploadFile(json, S3Configuration.BucketName, key, "application/json").ConfigureAwait(false);
        }

        [HttpDelete]
        [Route("scenes/{id}")]
        public async Task<bool> DeleteScene(string id)
        {
            var descriptor = await SceneDescriptorRepository.Get(id).ConfigureAwait(false);

            if (descriptor == null)
            {
                return false;
            }

            var key = WebUtility.UrlDecode(descriptor.StorageKey);
            var deleted = await CloudStorageService.DeleteFile(S3Configuration.BucketName, key).ConfigureAwait(false);

            if (deleted)
            {
                await SceneDescriptorRepository.Delete(descriptor.Id).ConfigureAwait(false);
            }

            return deleted;
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
