using System.Collections.Generic;
using System.Drawing;

namespace Core.Models.GameScenes
{
    public class Scene
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Scale { get; set; }
        public Point ViewportXY { get; set; }
        public List<SceneLayer> Layers { get; set; }
    }
}
