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
        public bool IsActive { get; set; } = false;
        public Dictionary<string, Sprite> Sprites { get; set; } = new Dictionary<string, Sprite>();
        public Dictionary<string, SceneGrid> Grids { get; set; } = new Dictionary<string, SceneGrid>();
    }
}
