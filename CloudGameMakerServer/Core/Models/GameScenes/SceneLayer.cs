using System.Collections.Generic;

namespace Core.Models.GameScenes
{
    public class SceneLayer
    {
        public List<List<SceneGrid>> Grids { get; set; } = new List<List<SceneGrid>>();
    }
}
