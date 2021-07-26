using sRPC.TCP;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Werewolf.Theme;
using Werewolf.Users.Api;

namespace Werewolf.Game
{
    public class UserController : UserFactory, IDisposable
    {
        public class UserApiNotification : UserNotificationServerBase
        {
            public override Task UpdatedUser(UserInfo request, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        private readonly TcpApiClient<UserApiClient, UserApiNotification> api;
        private bool disposedValue;

        public UserController(IPEndPoint userDbEndpoint)
        {
            api = new TcpApiClient<UserApiClient, UserApiNotification>(userDbEndpoint);
        }

        protected override async Task<UserInfo?> ReloadUser(UserId id)
        {
            await api.WaitConnect;
            return await api.RequestApi.GetUser(id);
        }

        public override async Task UpdateUserStats(UserId id, UserStats stats)
        {
            await api.WaitConnect;
            await api.RequestApi.UpdateStats(id, stats).CAF();
            _ = await ReloadUser(id).CAF();
        }

        public override async Task UpdateUserConfig(UserId id, UserConfig config)
        {
            await api.WaitConnect;
            var user = await GetUser(id, false).CAF();
            if (user is null)
                return;
            user.Config = config;
            await api.RequestApi.UpdateUser(user).CAF();
            UpdateCache(user);
        }

        /// <summary>
        /// Gets or create a user.
        /// </summary>
        /// <param name="ids">the ids of the connected system. One of them is required to find the user</param>
        /// <param name="config">this config will be set if the user needs to be created.</param>
        /// <returns>the found or created user</returns>
        public virtual async Task<UserInfo?> GetOrCreateAsync(UserConnectedIds ids, UserConfig config)
        {
            await api.WaitConnect;
            var id = await api.RequestApi.FindUser(ids);
            if (id is null)
                id = await api.RequestApi.CreateUser(connectedId: ids, config: config);
            return id is null ? null : await GetUser(id);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    api.Dispose();
                }
                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
