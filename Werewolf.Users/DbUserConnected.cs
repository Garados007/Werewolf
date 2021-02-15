namespace Werewolf.Users
{
    public class DbUserConnected
    {
        public ulong? DiscordId { get; set; }

        public DbUserConnected(){}

        public DbUserConnected(Api.UserConnectedIds api)
        {
            if (api.HasDiscordId)
                DiscordId = api.DiscordId;
            else DiscordId = null;
        }

        public Api.UserConnectedIds ToApi()
        {
            return new Api.UserConnectedIds
            {
                HasDiscordId = DiscordId.HasValue,
                DiscordId = DiscordId ?? 0,
            };
        }
    }
}