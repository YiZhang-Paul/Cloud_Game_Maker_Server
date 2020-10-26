using Amazon.S3;
using Amazon.S3.Model;
using Core.Models.GameSprites;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/cloud-storage")]
    public class CloudStorageController : ControllerBase
    {
        private const string BucketName = "cloud-game-maker";
        private IAmazonS3 S3 { get; set; }

        public CloudStorageController(IAmazonS3 s3)
        {
            S3 = s3;
        }

        [HttpGet]
        [Route("sprites")]
        public async Task<IEnumerable<SpriteFile>> GetSprites()
        {
            var response = await S3.ListObjectsAsync(BucketName, "sprites").ConfigureAwait(false);

            var tasks = response.S3Objects.Select(async _ =>
            {
                var s3Object = await S3.GetObjectAsync(BucketName, _.Key).ConfigureAwait(false);
                var bytes = new byte[(int)s3Object.ResponseStream.Length];

                using (var stream = new MemoryStream())
                {
                    await s3Object.ResponseStream.CopyToAsync(stream).ConfigureAwait(false);
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.Read(bytes, 0, (int)stream.Length);
                }

                return new SpriteFile
                {
                    Name = s3Object.Key,
                    Type = "",
                    Extension = "",
                    Base64 = Convert.ToBase64String(bytes)
                };
            });

            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        [HttpPost]
        [Route("sprites")]
        public async Task<bool> UploadSprite([FromBody]SpriteFile file)
        {
            if (string.IsNullOrWhiteSpace(file?.Base64))
            {
                return false;
            }

            try
            {
                var bytes = Convert.FromBase64String(file.Base64);

                using (var stream = new MemoryStream(bytes))
                {
                    var request = new PutObjectRequest
                    {
                        BucketName = BucketName,
                        Key = $"sprites/{file.Name}",
                        InputStream = stream
                    };

                    await S3.PutObjectAsync(request).ConfigureAwait(false);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
