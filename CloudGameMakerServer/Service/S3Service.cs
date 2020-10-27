using Amazon.S3;
using Amazon.S3.Model;
using Core.Services;
using Microsoft.AspNetCore.Http;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace Service
{
    public class S3Service : IS3Service
    {
        private IAmazonS3 S3 { get; set; }
        private IImageService ImageService { get; set; }

        public S3Service(IAmazonS3 s3, IImageService imageService)
        {
            S3 = s3;
            ImageService = imageService;
        }

        public async Task<string> UploadFile(IFormFile file, string bucket, string key, string mime)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream).ConfigureAwait(false);

                    var request = new PutObjectRequest
                    {
                        BucketName = bucket,
                        Key = key,
                        InputStream = stream,
                        ContentType = mime
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

        public async Task<string> GenerateThumbnail(IFormFile file, string bucket, string key, int width = 100, int height = 100)
        {
            try
            {
                var objectKey = $"thumbnails/{key}";

                using (var stream = new MemoryStream())
                {
                    var thumbnail = await ImageService.GetThumbnailImage(width, height, file).ConfigureAwait(false);
                    thumbnail.Save(stream, ImageFormat.Png);

                    var request = new PutObjectRequest
                    {
                        BucketName = bucket,
                        Key = objectKey,
                        InputStream = stream,
                        ContentType = "image/png"
                    };

                    await S3.PutObjectAsync(request).ConfigureAwait(false);
                }

                return objectKey;
            }
            catch
            {
                return null;
            }
        }
    }
}
