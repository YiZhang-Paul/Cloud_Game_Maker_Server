using Amazon.S3;
using Amazon.S3.Model;
using Core.Models.GameSprites;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
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
