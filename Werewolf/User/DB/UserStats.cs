using MongoDB.Bson.Serialization.Attributes;

namespace Werewolf.User.DB
{
    [BsonIgnoreExtraElements]
    public class UserStats
    {
        [BsonRequired]
        public uint WinGames { get; set; }

        [BsonRequired]
        public uint Killed { get; set; }

        [BsonRequired]
        public uint LooseGames { get; set; }

        [BsonRequired]
        public uint Leader { get; set; }

        [BsonRequired]
        public uint Level { get; set; }

        [BsonRequired]
        public ulong CurrentXp { get; set; }
    }
}