using System;
using System.Linq;
using MongoDB.Driver;
using MaxLib.Ini;

namespace Werewolf
{
    public class Database : IDisposable
    {
        private readonly MongoClient dbClient;
        private readonly IMongoDatabase database;

        public IMongoCollection<User.DB.UserInfo> UserInfo { get; }

        public Database(IniGroup config)
        {
            var target = config.GetString("connection", "mongodb://localhost");
            var settings = MongoClientSettings.FromConnectionString(target);
            settings.ApplicationName = config.GetString("application", "Werewolf");
            dbClient = new MongoClient(settings);
            database = dbClient.GetDatabase(config.GetString("database", "Werewolf"));
            UserInfo = database.GetCollection<User.DB.UserInfo>("user_info");
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}