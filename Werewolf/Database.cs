using System;
using System.Linq;
using MongoDB.Driver;

namespace Werewolf
{
    public class Database : IDisposable
    {
        private readonly MongoClient dbClient;
        private readonly IMongoDatabase database;

        public IMongoCollection<User.DB.UserInfo> UserInfo { get; }

        public Database()
        {
            var settings = MongoClientSettings.FromConnectionString("mongodb://localhost");
            settings.ApplicationName = "Werewolf";
            dbClient = new MongoClient(settings);
            database = dbClient.GetDatabase("Werewolf");
            UserInfo = database.GetCollection<User.DB.UserInfo>("user_info");
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}