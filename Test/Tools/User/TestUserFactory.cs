using System.Collections.Generic;
using System.Threading.Tasks;
using Werewolf.Theme.User;
using Werewolf.User;

namespace Test.Tools.User
{
    public class TestUserFactory : UserFactory
    {
        public Dictionary<UserId, UserInfo> KnownUser { get; }
            = new Dictionary<UserId, UserInfo>();

        public UserInfo NewUser()
        {
            var user = new TestUserInfo(KnownUser.Count);
            KnownUser.Add(user.Id, user);
            return user;
        }

        protected override Task<UserInfo?> ReloadUser(UserId id)
        {
            if (KnownUser.TryGetValue(id, out UserInfo? info))
                return Task.FromResult<UserInfo?>(info);
            else return Task.FromResult<UserInfo?>(null);
        }
    }
}
