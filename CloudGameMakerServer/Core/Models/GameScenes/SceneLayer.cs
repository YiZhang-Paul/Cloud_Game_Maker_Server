using Core.Models.GameSprites;
using System.Collections.Generic;

namespace Core.Models.GameScenes
{
    public class SceneLayer
    {
        public string Name { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public bool IsVisible { get; set; } = true;
        public Dictionary<string, SpriteFile> Sprites { get; set; } = new Dictionary<string, SpriteFile>();
        public Dictionary<string, SceneGrid> Grids { get; set; } = new Dictionary<string, SceneGrid>();
    }
}
