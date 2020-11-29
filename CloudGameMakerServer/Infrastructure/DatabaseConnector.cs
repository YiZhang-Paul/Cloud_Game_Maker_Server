using Core.Models;
using Core.Models.Configurations;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class DatabaseConnector<T> where T : DatabaseRecord
    {
        protected IMongoCollection<T> Collection { get; set; }

        public DatabaseConnector(IOptions<DatabaseConfiguration> configuration, string collection)
        {
            Collection = Connect(configuration.Value, collection);
        }

        public async Task<IEnumerable<T>> Get(int limit = 0)
        {
            return await Collection.Find(new BsonDocument()).Limit(limit).ToListAsync().ConfigureAwait(false);
        }

        public async Task<T> Get(string id)
        {
            var filter = Builders<T>.Filter.Eq(_ => _.Id, id);

            return await Collection.Find(filter).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task Add(T document)
        {
            await Collection.InsertOneAsync(document).ConfigureAwait(false);
        }

        public async Task Replace(T document)
        {
            var filter = Builders<T>.Filter.Eq(_ => _.Id, document.Id);

            await Collection.ReplaceOneAsync(filter, document).ConfigureAwait(false);
        }

        public async Task Delete(string id)
        {
            var filter = Builders<T>.Filter.Eq(_ => _.Id, id);

            await Collection.DeleteOneAsync(filter).ConfigureAwait(false);
        }

        private IMongoCollection<T> Connect(DatabaseConfiguration configuration, string collection)
        {
            var database = new MongoClient(configuration.Url).GetDatabase(configuration.Name);

            return database.GetCollection<T>(collection);
        }
    }
}
