namespace Core.Models.GameSprites
{
    public class SpriteFile
    {
        public string Originated { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public byte[] Content { get; set; }
        public string Mime { get; set; }
        public string Extension { get; set; }
        public string Base64 { get; set; }
    }
}
