namespace Core.Models.Configurations
{
    public class S3Configuration
    {
        public const string Key = "AWS:S3";
        public string BucketName { get; set; }
        public double UrlTimeAlive { get; set; }
        public void Deconstruct(out string a, out double b) => (a, b) = (BucketName, UrlTimeAlive);
    }
}
