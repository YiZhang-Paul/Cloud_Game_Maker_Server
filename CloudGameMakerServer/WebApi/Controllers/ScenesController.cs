using Core.Models.GameScenes;
using Core.Services;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/v1/scenes")]
    public class ScenesController : ControllerBase
    {
        private SceneDescriptorRepository SceneDescriptorRepository { get; set; }
        private IGameSceneService GameSceneService { get; set; }

        public ScenesController
        (
            SceneDescriptorRepository sceneDescriptorRepository,
            IGameSceneService gameSceneService
        )
        {
            SceneDescriptorRepository = sceneDescriptorRepository;
            GameSceneService = gameSceneService;
        }

        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<SceneDescriptor>> GetSceneDescriptors([FromQuery] int limit = 0)
        {
            return await SceneDescriptorRepository.Get(limit).ConfigureAwait(false);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<Scene> GetScene(string id)
        {
            return await GameSceneService.GetScene(id).ConfigureAwait(false);
        }

        [HttpPost]
        [Route("")]
        public async Task<SceneDescriptor> AddScene([FromBody] Scene scene)
        {
            return await GameSceneService.AddScene(scene).ConfigureAwait(false);
        }

        [HttpPut]
        [Route("")]
        public async Task<bool> UpdateScene([FromBody] Scene scene)
        {
            return await GameSceneService.UpdateScene(scene).ConfigureAwait(false);
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<bool> DeleteScene(string id)
        {
            return await GameSceneService.DeleteScene(id).ConfigureAwait(false);
        }
    }
}
