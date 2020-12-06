using Core.Models.GameSprites;
using System.Collections.Generic;
using System.Drawing;

namespace Core.Models.GameScenes
{
    public class Scene
    {
        public string StorageKey { get; set; }
        public string Name { get; set; }
        public int Scale { get; set; }
        public Point ViewportXY { get; set; }
        public Dictionary<string, Sprite> Sprites { get; set; } = new Dictionary<string, Sprite>();
        public List<SceneLayer> Layers { get; set; }
    }
}
