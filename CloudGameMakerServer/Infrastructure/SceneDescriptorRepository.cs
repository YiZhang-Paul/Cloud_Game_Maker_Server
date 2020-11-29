using Core.Models.Configurations;
using Core.Models.GameScenes;
using Microsoft.Extensions.Options;

namespace Infrastructure
{
    public class SceneDescriptorRepository : DatabaseConnector<SceneDescriptor>
    {
        public SceneDescriptorRepository(IOptions<DatabaseConfiguration> configuration) : base(configuration, "Scene") { }
    }
}
