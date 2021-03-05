using System;
using LiteDB;

namespace Werewolf.Users
{
    public class Database : IDisposable
    {
        private readonly LiteDatabase database;
        public ILiteCollection<DbUser> User { get; }

        public Database(string path)
        {
            _ = path ?? throw new ArgumentNullException(nameof(path));
            database = new LiteDatabase(path);

            if (database.UserVersion == 0)
            {
                var user = database.GetCollection("user");
                _ = user.DropIndex("DiscordId");
                _ = user.UpdateMany(
                    BsonExpression.Create("{UserId:{Source:\"Discord\",_id:$.DiscordId}}"),
                    BsonExpression.Create("1=1")
                );
                database.UserVersion = 1;
            }

            User = database.GetCollection<DbUser>("user");
        }

        public void Dispose()
        {
            database.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}