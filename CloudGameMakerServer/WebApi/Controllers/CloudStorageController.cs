using Amazon.S3;
using Amazon.S3.Model;
using Core.Models.GameSprites;
using Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
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
        private IAmazonS3 S3 { get; set; }
        private IImageService ImageService { get; set; }

        public CloudStorageController(IAmazonS3 s3, IImageService imageService)
        {
            S3 = s3;
            ImageService = imageService;
        }

        [HttpGet]
        [Route("sprites")]
        public async Task<IEnumerable<SpriteFile>> GetSprites()
        {
            var response = await S3.ListObjectsAsync(BucketName, "sprites").ConfigureAwait(false);

            return response.S3Objects.Select(_ =>
            {
                var isPng = Regex.IsMatch(_.Key, "\\.png$");

                var originalRequest = new GetPreSignedUrlRequest
                {
                    BucketName = BucketName,
                    Key = _.Key,
                    Expires = DateTime.UtcNow.AddHours(2)
                };

                var thumbnailRequest = new GetPreSignedUrlRequest
                {
                    BucketName = BucketName,
                    Key = $"thumbnails/{_.Key}",
                    Expires = DateTime.UtcNow.AddHours(2)
                };

                return new SpriteFile
                {
                    Id = _.Key,
                    Name = Regex.Replace(_.Key, $"^.*/|\\.(jpg|png)$", string.Empty),
                    Mime = isPng ? "image/png" : "image/jpeg",
                    Extension = isPng ? "png" : "jpg",
                    OriginalUrl = S3.GetPreSignedURL(originalRequest),
                    ThumbnailUrl = S3.GetPreSignedURL(thumbnailRequest)
                };
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

            try
            {
                var key = $"sprites/{sprite.Name}.{sprite.Extension}";

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream).ConfigureAwait(false);

                    var request = new PutObjectRequest
                    {
                        BucketName = BucketName,
                        Key = key,
                        InputStream = stream,
                        ContentType = sprite.Mime
                    };

                    await S3.PutObjectAsync(request).ConfigureAwait(false);
                }

                using (var stream = new MemoryStream())
                {
                    var thumbnail = await ImageService.GetThumbnailImage(100, 100, file).ConfigureAwait(false);
                    thumbnail.Save(stream, ImageFormat.Png);

                    var request = new PutObjectRequest
                    {
                        BucketName = BucketName,
                        Key = $"thumbnails/{key}",
                        InputStream = stream,
                        ContentType = "image/png"
                    };

                    await S3.PutObjectAsync(request).ConfigureAwait(false);
                }

                return key;
            }
            catch
            {
                return null;
            }
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
            try
            {
                var key = WebUtility.UrlDecode(id);
                // will throw error when object does not exist
                await S3.GetObjectMetadataAsync(BucketName, key).ConfigureAwait(false);
                await S3.DeleteObjectAsync(BucketName, key).ConfigureAwait(false);
                _ = S3.DeleteObjectAsync(BucketName, $"thumbnails/{key}").ConfigureAwait(false);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
