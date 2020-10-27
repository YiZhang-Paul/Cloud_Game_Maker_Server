using Amazon.S3;
using Amazon.S3.Model;
using Core.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace Service
{
    public class S3Service : IS3Service
    {
        private const string ThumbnailFolder = "thumbnails";
        private IAmazonS3 S3 { get; set; }
        private IImageService ImageService { get; set; }

        public S3Service(IAmazonS3 s3, IImageService imageService)
        {
            S3 = s3;
            ImageService = imageService;
        }

        public string GetPreSignedURL(string bucket, string key, double hours)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = key,
                Expires = DateTime.UtcNow.AddHours(hours)
            };

            return S3.GetPreSignedURL(request);
        }

        public string GetThumbnailPreSignedURL(string bucket, string key, double hours)
        {
            return GetPreSignedURL(bucket, $"{ThumbnailFolder}/{key}", hours);
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
                var objectKey = $"{ThumbnailFolder}/{key}";

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

        public async Task<bool> DeleteThumbnail(string bucket, string key)
        {
            return await DeleteFile(bucket, $"{ThumbnailFolder}/{key}", false).ConfigureAwait(false);
        }

        public async Task<bool> DeleteFile(string bucket, string key, bool ensureDelete = true)
        {
            try
            {
                if (ensureDelete)
                {
                    // will throw error if object does not exist
                    await S3.GetObjectMetadataAsync(bucket, key).ConfigureAwait(false);
                }

                await S3.DeleteObjectAsync(bucket, key).ConfigureAwait(false);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
