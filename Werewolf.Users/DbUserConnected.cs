namespace Werewolf.Users
{
    public class DbUserConnected
    {
        public ulong? DiscordId { get; set; }

        public DbUserConnected() { }

        public DbUserConnected(Api.UserConnectedIds api)
        {
            DiscordId = api.HasDiscordId ? api.DiscordId : (ulong?)null;
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