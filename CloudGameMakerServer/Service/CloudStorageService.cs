using Amazon.S3;
using Amazon.S3.Model;
using Core.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Service
{
    public class CloudStorageService : ICloudStorageService
    {
        private const string ThumbnailFolder = "thumbnails";
        private IAmazonS3 S3 { get; set; }
        private IImageService ImageService { get; set; }

        public CloudStorageService(IAmazonS3 s3, IImageService imageService)
        {
            S3 = s3;
            ImageService = imageService;
        }

        public string GetPreSignedUrl(string bucket, string key, double hours)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = key,
                Expires = DateTime.UtcNow.AddHours(hours)
            };

            return S3.GetPreSignedURL(request);
        }

        public string GetThumbnailPreSignedUrl(string bucket, string key, double hours)
        {
            return GetPreSignedUrl(bucket, $"{ThumbnailFolder}/{key}", hours);
        }

        public bool IsPreSignedUrlExpired(string url)
        {
            var timeAliveString = Regex.Match(url, @"(?<=X-Amz-Expires=)[^&]+").Value;
            var creationDateString = Regex.Match(url, @"(?<=X-Amz-Date=)[^Z]+").Value;

            if (timeAliveString == null || creationDateString == null)
            {
                return false;
            }

            var isValidTime = double.TryParse(timeAliveString, out var timeAlive);
            var isValidDate = DateTime.TryParseExact(creationDateString, "yyyyMMddTHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var creationDate);

            return isValidTime && isValidDate && creationDate.AddSeconds(timeAlive) < DateTime.UtcNow;
        }

        public async Task<IEnumerable<S3Object>> GetMetas(string bucket, string folder)
        {
            return (await S3.ListObjectsAsync(bucket, folder).ConfigureAwait(false)).S3Objects;
        }

        public async Task<Stream> GetFile(string bucket, string key)
        {
            return (await S3.GetObjectAsync(bucket, key).ConfigureAwait(false)).ResponseStream;
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

        public async Task<string> UploadFile(string content, string bucket, string key, string mime)
        {
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = key,
                    ContentBody = content
                };

                await S3.PutObjectAsync(request).ConfigureAwait(false);

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
                    thumbnail.Save(stream, ImageFormat.Jpeg);

                    var request = new PutObjectRequest
                    {
                        BucketName = bucket,
                        Key = objectKey,
                        InputStream = stream,
                        ContentType = "image/jpeg"
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
