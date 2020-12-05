using Core.Models.Configurations;
using Core.Models.GameScenes;
using Core.Services;
using Infrastructure;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Service
{
    public class GameSceneService : IGameSceneService
    {
        private S3Configuration S3Configuration { get; set; }
        private SceneDescriptorRepository SceneDescriptorRepository { get; set; }
        private ICloudStorageService CloudStorageService { get; set; }

        public GameSceneService
        (
            IOptions<S3Configuration> s3Configuration,
            SceneDescriptorRepository sceneDescriptorRepository,
            ICloudStorageService cloudStorageService
        )
        {
            S3Configuration = s3Configuration.Value;
            SceneDescriptorRepository = sceneDescriptorRepository;
            CloudStorageService = cloudStorageService;
        }

        public async Task<Scene> GetScene(string id)
        {
            var descriptor = await SceneDescriptorRepository.Get(id).ConfigureAwait(false);

            if (descriptor == null)
            {
                return null;
            }

            var key = WebUtility.UrlDecode(descriptor.StorageKey);
            var file = await CloudStorageService.GetFile(S3Configuration.BucketName, key).ConfigureAwait(false);

            using (var reader = new StreamReader(file))
            {
                var option = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var scene = JsonSerializer.Deserialize<Scene>(reader.ReadToEnd(), option);
                scene.StorageKey = key;

                return RenewPreSignedUrls(scene);
            }
        }

        public async Task<SceneDescriptor> AddScene(Scene scene)
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

        public async Task<bool> UpdateScene(Scene scene)
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

        private Scene RenewPreSignedUrls(Scene scene)
        {
            foreach (var spriteId in scene.Sprites.Keys)
            {
                var url = scene.Sprites[spriteId].ThumbnailUrl;

                if (!CloudStorageService.IsPreSignedUrlExpired(url))
                {
                    continue;
                }

                var (bucketName, urlTimeAlive) = S3Configuration;
                var renewed = CloudStorageService.GetThumbnailPreSignedUrl(bucketName, spriteId, urlTimeAlive);
                scene.Sprites[spriteId].ThumbnailUrl = renewed;
            }

            return scene;
        }
    }
}
