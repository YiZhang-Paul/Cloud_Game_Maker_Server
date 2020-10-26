using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<bool> UploadFile()
        {
            var file = Request.Form.Files?[0];

            if (file == null || file.Length == 0)
            {
                return false;
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream).ConfigureAwait(false);

                    var request = new PutObjectRequest
                    {
                        BucketName = BucketName,
                        Key = $"uploads/{file.Name}",
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
