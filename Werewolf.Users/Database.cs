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
                    BsonExpression.Create("{OAuthId:\"Discord:\"+$.DiscordId}"),
                    // BsonExpression.Create("{UserId:{Source:\"Discord\",_id:$.DiscordId}}"),
                    BsonExpression.Create("1=1")
                );
                database.UserVersion = 2;
            }
            if (database.UserVersion == 1)
            {
                var user = database.GetCollection("user");
                // this invalidates all discord ids. The new OAuth ids are no valid OAuth ids
                // and therefore no accounts can be matched. An admin should replace the marker
                // with the actual id.
                _ = user.UpdateMany(
                    BsonExpression.Create("{UserId:null,OAuthId:\"Discord:\"+$.UserId._id}"),
                    BsonExpression.Create("1=1")
                );
                database.UserVersion = 2;
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