using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Werewolf.User;

public class UserController(Database database, string oAuthUserInfoEndpoint) : UserFactory, IDisposable
{
    public Database Database { get; } = database;

    public string OAuthUserInfoEndpoint { get; } = oAuthUserInfoEndpoint;
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
        using var hc = new HttpClient();
        hc.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenString);
        JsonDocument json;
        try
        {
            using var m = await hc.GetStreamAsync(OAuthUserInfoEndpoint).CAF();
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
        {
            // update information
            string? value;
            if ((value = GetSaneStringFromJson(json.RootElement, "picture", VerfifyUrl) ??
                    GenerateRandomAvatar()
                ) is not null
                && value != user.Config.Image)
                await user.Config.SetImageAsync(value).CAF();
            if ((value = GetSaneStringFromJson(json.RootElement, "game_username") ??
                    GetSaneStringFromJson(json.RootElement, "preferred_username")) is not null
                && value != user.Config.Username)
                await user.Config.SetUsernameAsync(value).CAF();
            return user;
        }

        // create user
        user = await CreateAsync(
            subId,
            new DB.UserConfig
            {
                Image = GetSaneStringFromJson(json.RootElement, "picture", VerfifyUrl) ??
                    GenerateRandomAvatar(),
                Language = GetSaneStringFromJson(json.RootElement, "locale") ?? "en",
                Username = GetSaneStringFromJson(json.RootElement, "game_username") ??
                    GetSaneStringFromJson(json.RootElement, "preferred_username") ??
                    "missing-username",
                ThemeColor = "#333333",
                BackgroundImage = null,
            }
        ).CAF();
        if (user is not null)
            return user;

        // nevermind it doesn't work
        Serilog.Log.Warning("Cannot create new user. Check your configuration settings.");
        return null;
    }

    private static string? GetSaneStringFromJson(JsonElement json, string name,
        Func<string, bool>? extraCheck = null)
    {
        if (!json.TryGetProperty(name, out JsonElement node))
            return null;
        var value = node.GetString();
        if (value is null || value == "")
            return null;
        if (extraCheck is not null && !extraCheck(value))
            return null;
        return value;
    }

    private static bool VerfifyUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return false;
        return uri.Scheme.ToLower() == "http" || uri.Scheme.ToLower() == "https";
    }

    public static string GenerateRandomAvatar()
    {
        Span<byte> buffer = stackalloc byte[15];
        Random.Shared.NextBytes(buffer);
        return "@" + Convert.ToBase64String(buffer);
    }
}
