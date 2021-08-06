using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Werewolf.User.DB
{
    [BsonIgnoreExtraElements]
    public class UserInfo
    {
        [BsonId, BsonRequired]
        public ObjectId Id { get; set; }

        public string? OAuthId { get; set; }

        [BsonRequired]
        public UserConfig Config { get; set; } = new UserConfig();

        [BsonRequired]
        public UserStats Stats { get; set; } = new UserStats();
    }
}