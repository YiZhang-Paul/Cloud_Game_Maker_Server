using Core.Models.Configurations;
using Core.Models.GameScenes;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class SceneDescriptorRepository : DatabaseConnector<SceneDescriptor>
    {
        public SceneDescriptorRepository(IOptions<DatabaseConfiguration> configuration) : base(configuration, "Scene") { }

        public async Task<SceneDescriptor> GetByStorageKey(string key)
        {
            var filter = Builders<SceneDescriptor>.Filter.Eq(_ => _.StorageKey, key);

            return await Collection.Find(filter).FirstOrDefaultAsync().ConfigureAwait(false);
        }
    }
}
