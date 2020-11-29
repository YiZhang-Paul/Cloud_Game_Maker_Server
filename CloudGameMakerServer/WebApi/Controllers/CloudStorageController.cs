using Core.Models.GameScenes;
using Core.Models.GameSprites;
using Core.Services;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
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
        private const double UrlTimeAlive = 8;
        private ICloudStorageService CloudStorageService { get; set; }
        private SceneDescriptorRepository SceneDescriptorRepository { get; set; }

        public CloudStorageController(ICloudStorageService cloudStorageService, SceneDescriptorRepository sceneDescriptorRepository)
        {
            CloudStorageService = cloudStorageService;
            SceneDescriptorRepository = sceneDescriptorRepository;
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
            var descriptor = await SceneDescriptorRepository.GetByStorageKey(id).ConfigureAwait(false);

            if (descriptor == null)
            {
                return null;
            }

            var key = WebUtility.UrlDecode(descriptor.StorageKey);
            var file = await CloudStorageService.GetFile(BucketName, key).ConfigureAwait(false);

            using (var reader = new StreamReader(file))
            {
                var option = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var scene = JsonSerializer.Deserialize<Scene>(reader.ReadToEnd(), option);
                scene.StorageKey = key;

                foreach (var layer in scene.Layers)
                {
                    foreach (var spriteId in layer.Sprites.Keys)
                    {
                        var url = layer.Sprites[spriteId].ThumbnailUrl;

                        if (CloudStorageService.IsPreSignedUrlExpired(url))
                        {
                            layer.Sprites[spriteId].ThumbnailUrl = CloudStorageService.GetThumbnailPreSignedUrl(BucketName, spriteId, UrlTimeAlive);
                        }
                    }
                }

                return scene;
            }
        }

        [HttpPost]
        [Route("scenes")]
        public async Task<string> AddScene([FromBody]Scene scene)
        {
            var key = await UploadScene(scene, $"scenes/{Guid.NewGuid()}.json").ConfigureAwait(false);

            if (key != null)
            {
                await SceneDescriptorRepository.Add(new SceneDescriptor { StorageKey = key, Name = scene.Name });
            }

            return key;
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

            return await CloudStorageService.UploadFile(json, BucketName, key, "application/json").ConfigureAwait(false);
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
            var deleted = await CloudStorageService.DeleteFile(BucketName, key).ConfigureAwait(false);

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
            var file = await CloudStorageService.GetFile(BucketName, key).ConfigureAwait(false);

            return File(file, "image/jpeg");
        }

        [HttpGet]
        [Route("sprites")]
        public async Task<IEnumerable<Sprite>> GetSprites()
        {
            var metas = await CloudStorageService.GetMetas(BucketName, "sprites").ConfigureAwait(false);

            return metas.Select(_ => new Sprite
            {
                Id = _.Key,
                Name = Regex.Replace(_.Key, $"^.*/|\\.jpg$", string.Empty),
                Mime = "image/jpeg",
                Extension = "jpg",
                OriginalUrl = CloudStorageService.GetPreSignedUrl(BucketName, _.Key, UrlTimeAlive),
                ThumbnailUrl = CloudStorageService.GetThumbnailPreSignedUrl(BucketName, _.Key, UrlTimeAlive)
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

            var option = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var sprite = JsonSerializer.Deserialize<Sprite>(spriteJson, option);
            var key = $"sprites/{sprite.Name}.{sprite.Extension}";
            await CloudStorageService.GenerateThumbnail(file, BucketName, key).ConfigureAwait(false);
            sprite.Id = await CloudStorageService.UploadFile(file, BucketName, key, sprite.Mime).ConfigureAwait(false);

            if (sprite.Id == null)
            {
                return null;
            }

            sprite.OriginalUrl = CloudStorageService.GetPreSignedUrl(BucketName, sprite.Id, UrlTimeAlive);
            sprite.ThumbnailUrl = CloudStorageService.GetThumbnailPreSignedUrl(BucketName, sprite.Id, UrlTimeAlive);

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
            _ = CloudStorageService.DeleteThumbnail(BucketName, key).ConfigureAwait(false);

            return await CloudStorageService.DeleteFile(BucketName, key).ConfigureAwait(false);
        }
    }
}
