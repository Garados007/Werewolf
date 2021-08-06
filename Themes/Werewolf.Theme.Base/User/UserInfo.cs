namespace Werewolf.User
{
    public abstract class UserInfo
    {
        public abstract UserId Id { get; }

        public abstract string? OAuthId { get; }

        public bool IsGuest => OAuthId is null || OAuthId.Length == 0;

        public abstract UserConfig Config { get; }

        public abstract UserStats Stats { get; }
    }
}