using Core.Models.Configurations;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Infrastructure
{
    public class DatabaseConnector<T>
    {
        protected IMongoCollection<T> Collection { get; set; }

        public DatabaseConnector(IOptions<DatabaseConfiguration> configuration, string collection)
        {
            Collection = Connect(configuration.Value, collection);
        }

        public IMongoCollection<T> Connect(DatabaseConfiguration configuration, string collection)
        {
            var database = new MongoClient(configuration.Url).GetDatabase(configuration.Name);

            return database.GetCollection<T>(collection);
        }
    }
}
