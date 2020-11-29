using MongoDB.Driver;

namespace Infrastructure
{
    public class DatabaseConnector<T>
    {
        protected IMongoCollection<T> Collection { get; set; }

        public DatabaseConnector(string url, string collection)
        {
            Collection = Connect(url, collection);
        }

        public IMongoCollection<T> Connect(string url, string collection)
        {
            var database = new MongoClient(url).GetDatabase("cloud_game_maker");

            return database.GetCollection<T>(collection);
        }
    }
}
