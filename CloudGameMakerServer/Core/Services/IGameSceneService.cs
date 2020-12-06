using Core.Models.GameScenes;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface IGameSceneService
    {
        Task<Scene> GetScene(string id);
        Task<SceneDescriptor> AddScene(Scene scene);
        Task<bool> UpdateScene(Scene scene);
        Task<bool> DeleteScene(string id);
    }
}
