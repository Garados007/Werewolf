using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Werewolf.User
{
    public abstract class UserFactory
    {
        private readonly ConcurrentDictionary<UserId, UserInfo> users
            = new ConcurrentDictionary<UserId, UserInfo>();

        public async Task<UserInfo?> GetUser(UserId id, bool allowCache = true)
        {
            UserInfo? user;
            if (allowCache && (user = GetCachedUser(id)) != null)
                return user;
            user = await ReloadUser(id);
            if (user != null)
                UpdateCache(user);
            return user;
        }

        public UserInfo? GetCachedUser(UserId id)
        {
            return users.TryGetValue(id, out UserInfo? value) ? value : null;
        }

        protected abstract Task<UserInfo?> ReloadUser(UserId id);

        protected void UpdateCache(UserInfo user)
        {
            _ = users.AddOrUpdate(user.Id, user, (_, _) => user);
        }

        public bool RemoveCachedGuest(UserId id)
        {
            if (GetCachedUser(id)?.IsGuest ?? false)
            {
                return users.TryRemove(id, out _); 
            }
            else return false;
        }
    }
}
