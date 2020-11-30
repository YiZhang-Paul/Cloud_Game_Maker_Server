using Core.Models.Configurations;
using Core.Models.GameScenes;
using Core.Services;
using Infrastructure;
using Microsoft.Extensions.Options;
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

        private Scene RenewPreSignedUrls(Scene scene)
        {
            foreach (var layer in scene.Layers)
            {
                foreach (var spriteId in layer.Sprites.Keys)
                {
                    var url = layer.Sprites[spriteId].ThumbnailUrl;

                    if (!CloudStorageService.IsPreSignedUrlExpired(url))
                    {
                        continue;
                    }

                    var (bucketName, urlTimeAlive) = S3Configuration;
                    var renewed = CloudStorageService.GetThumbnailPreSignedUrl(bucketName, spriteId, urlTimeAlive);
                    layer.Sprites[spriteId].ThumbnailUrl = renewed;
                }
            }

            return scene;
        }
    }
}
