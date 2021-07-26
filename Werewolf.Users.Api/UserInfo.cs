namespace Werewolf.Users.Api
{
    public partial class UserInfo
    {
        public bool IsGuest
            => OauthId is null ||
                OauthId.Id is null ||
                OauthId.Id.Length == 0;
    }
}