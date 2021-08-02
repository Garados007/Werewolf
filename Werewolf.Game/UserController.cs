using sRPC.TCP;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
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

        public string OAuthUserInfoEndpoint { get; }

        public UserController(IPEndPoint userDbEndpoint, string oAuthUserInfoEndpoint)
        {
            api = new TcpApiClient<UserApiClient, UserApiNotification>(userDbEndpoint);
            OAuthUserInfoEndpoint = oAuthUserInfoEndpoint;
        }

        protected override async Task<UserInfo?> ReloadUser(UserId id)
        {
            await api.WaitConnect.CAF();
            return await api.RequestApi.GetUser(id).CAF();
        }

        public override async Task UpdateUserStats(UserId id, UserStats stats)
        {
            await api.WaitConnect.CAF();
            await api.RequestApi.UpdateStats(id, stats).CAF();
            _ = await ReloadUser(id).CAF();
        }

        public override async Task UpdateUserConfig(UserId id, UserConfig config)
        {
            await api.WaitConnect.CAF();
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
        public virtual async Task<UserInfo?> GetOrCreateAsync(OAuthId oid, UserConfig config)
        {
            await api.WaitConnect.CAF();
            var id = await api.RequestApi.FindUser(oid);
            if (id is null)
                id = await api.RequestApi.CreateUser(oauthId: oid, config: config).CAF();
            return id is null ? null : await GetUser(id).CAF();
        }

        public async Task<UserInfo?> GetUserFromToken(string tokenString)
        {
            // parse and validate token
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken token;
            try { token = handler.ReadJwtToken(tokenString); }
            catch (Exception e)
            {
                Serilog.Log.Error(e, "cannot parse token");
                return null;
            }
            if (token.ValidTo < DateTime.UtcNow || token.ValidFrom > DateTime.UtcNow)
                return null;
            
            // ask identity server for validation (this includes certificate check)
            using var wc = new WebClient();
            wc.Headers.Add(
                HttpRequestHeader.Authorization,
                $"Bearer {tokenString}"
            );
            JsonDocument json;
            try 
            {
                var webResponse = await wc.DownloadDataTaskAsync(OAuthUserInfoEndpoint).CAF(); 
                using var m = new System.IO.MemoryStream(webResponse);
                json = await JsonDocument.ParseAsync(m).CAF();
            }
            catch (WebException)
            {
                return null;
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, "error while fetching OAuth userinfo");
                return null;
            }

            // check for id
            Serilog.Log.Verbose("[UserFromToken] wait for db");
            await api.WaitConnect.CAF();
            Serilog.Log.Verbose("[UserFromToken] db ready");
            if (!json.RootElement.TryGetProperty("sub", out JsonElement node))
                return null;
            var subId = node.GetString();
            if (subId is null)
                return null;
            var userId = await api.RequestApi.FindUser(new OAuthId { Id = subId }).CAF();
            Serilog.Log.Verbose("[UserFromToken] found user id {id}", userId);
            if (userId is not null)
            {
                var info = await GetUser(userId).CAF();
                if (info is not null)
                    return info;
            }
            
            // create user
            userId = await api.RequestApi.CreateUser(new UserInfo
            {
                OauthId = new OAuthId { Id = subId },
                Config = new UserConfig
                {
                    Image = json.RootElement.TryGetProperty("picture", out node) ?
                        node.GetString() : null,
                    Language = json.RootElement.TryGetProperty("locale", out node) ?
                        node.GetString() : "en",
                    Username = json.RootElement.TryGetProperty("preferred_username", out node) ?
                        node.GetString() : null,
                    ThemeColor = "#ffffff",
                    BackgroundImage = null,
                },
                Stats = new UserStats(),
            }).CAF();
            if (userId is not null)
            {
                var info = await GetUser(userId).CAF();
                if (info is not null)
                    return info;
            }

            // nevermind it doesn't work
            Serilog.Log.Warning("Cannot create new user. Check your configuration settings.");
            return null;
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
