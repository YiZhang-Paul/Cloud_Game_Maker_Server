using Core.Models.GameScenes;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface IGameSceneService
    {
        Task<Scene> GetScene(string id);
    }
}
