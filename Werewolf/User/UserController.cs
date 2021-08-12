using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Werewolf.User
{
    public class UserController : UserFactory, IDisposable
    {
        public Database Database { get; }

        public UserController(Database database, string oAuthUserInfoEndpoint)
        {
            Database = database;
            OAuthUserInfoEndpoint = oAuthUserInfoEndpoint;
        }

        public string OAuthUserInfoEndpoint { get; }
        private bool disposedValue;


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected override async Task<UserInfo?> ReloadUser(UserId id)
        {
            var entry = await (await Database.UserInfo.FindAsync(
                Builders<DB.UserInfo>.Filter.Eq("_id", id.Id),
                new FindOptions<DB.UserInfo, DB.UserInfo>
                {
                    Limit = 1,
                }
            ).CAF()).FirstOrDefaultAsync().CAF();
            if (entry is null)
                return null;
            return new UserInfoImpl(Database, entry);
        }

        protected virtual async Task<UserInfo?> FindUser(string oAuthId)
        {
            var entry = await (await Database.UserInfo.FindAsync(
                Builders<DB.UserInfo>.Filter.Eq("OAuthId", oAuthId),
                new FindOptions<DB.UserInfo, DB.UserInfo>
                {
                    Limit = 1,
                }
            ).CAF()).FirstOrDefaultAsync().CAF();
            if (entry is null)
                return null;
            return new UserInfoImpl(Database, entry);
        }

        public virtual async Task<UserInfo?> CreateAsync(string? oAuthId, DB.UserConfig config)
        {
            var id = ObjectId.GenerateNewId();
            var user = new DB.UserInfo
            {
                Id = id,
                Config = config,
                OAuthId = oAuthId,
                Stats = new DB.UserStats(),
            };
            if (oAuthId is not null)
                await Database.UserInfo.InsertOneAsync(user).CAF();
            else base.UpdateCache(new UserInfoImpl(Database, user));
            return await GetUser(new UserId(id)).CAF();
        }

        public virtual async Task<UserInfo?> GetOrCreateAsync(string oAuthId, DB.UserConfig config)
        {
            var user = await FindUser(oAuthId).CAF();
            if (user is not null)
            {
                UpdateCache(user);
                return user;
            }
            return await CreateAsync(oAuthId, config).CAF();
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
            if (!json.RootElement.TryGetProperty("sub", out JsonElement node))
                return null;
            var subId = node.GetString();
            if (subId is null)
                return null;
            var user = await FindUser(subId).CAF();
            if (user is not null)
                return user;
            
            // create user
            user = await CreateAsync(
                subId,
                new DB.UserConfig
                {
                    Image = (json.RootElement.TryGetProperty("picture", out node) ?
                        node.GetString() : null) ??
                        GravatarLinkFromEmail(json.RootElement.TryGetProperty("email", out node) ? 
                        node.GetString() : null),
                    Language = json.RootElement.TryGetProperty("locale", out node) ?
                        node.GetString() ?? "en" : "en",
                    Username = json.RootElement.TryGetProperty("preferred_username", out node) ?
                        node.GetString() ?? "missing-username" : "missing-username",
                    ThemeColor = "#ffffff",
                    BackgroundImage = null,
                }
            ).CAF();
            if (user is not null)
                return user;

            // nevermind it doesn't work
            Serilog.Log.Warning("Cannot create new user. Check your configuration settings.");
            return null;
        }

        public static string GravatarLinkFromEmail(string? email)
        {
            var hex = email is null ? "" :
                BitConverter.ToString(
                    System.Security.Cryptography.MD5.HashData(
                        System.Text.Encoding.UTF8.GetBytes(email)
                    )
                ).Replace("-", "");
            return $"https://www.gravatar.com/avatar/{hex}?d=identicon";
        }
    }
}