using LiteDB;

namespace Werewolf.Users
{
    public class DbUser
    {
        public ObjectId Id { get; set; } = new ObjectId();

        public string OAuthId { get; set; } = "";

        public DbUserConfig Config { get; set; }
            = new DbUserConfig();

        public DbUserStats Stats { get; set; }
            = new DbUserStats();

        public DbUser() { }

        public DbUser(Api.UserInfo api)
        {
            Id = new ObjectId(api.Id.Id.ToByteArray());
            OAuthId = api.OauthId.Id;
            Config = new DbUserConfig(api.Config);
            Stats = new DbUserStats(api.Stats);
        }

        public Api.UserInfo ToApi()
        {
            return new Api.UserInfo
            {
                Id = new Api.UserId
                {
                    Id = Google.Protobuf.ByteString.CopyFrom(Id.ToByteArray()),
                },
                OauthId = new Api.OAuthId
                {
                    Id = OAuthId,
                },
                Config = Config.ToApi(),
                Stats = Stats.ToApi(),
            };
        }
    }
}