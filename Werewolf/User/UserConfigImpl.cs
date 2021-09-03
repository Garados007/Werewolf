using System.Threading.Tasks;
using MongoDB.Driver;

namespace Werewolf.User
{
    public sealed class UserConfigImpl : UserConfig
    {
        public UserInfoImpl Info { get; }

        public UserConfigImpl(UserInfoImpl info)
            => Info = info;

        public override string Username => Info.DB.Config.Username;

        public override string Image => Info.DB.Config.Image;

        public override string ThemeColor => Info.DB.Config.ThemeColor;

        public override string? BackgroundImage => Info.DB.Config.BackgroundImage;

        public override string Language => Info.DB.Config.Language;

        public override async ValueTask SetUsernameAsync(string username)
        {
            if (username == Username)
                return;
            if (!Info.IsGuest)
                await Info.Database.UserInfo.UpdateOneAsync(
                    Builders<DB.UserInfo>.Filter.Eq("_id", Info.DB.Id),
                    Builders<DB.UserInfo>.Update.Set("Config.Username", username)
                ).CAF();
            Info.DB.Config.Username = username;
        }

        public override async ValueTask SetImageAsync(string image)
        {
            if (image == Image)
                return;
            if (!Info.IsGuest)
                await Info.Database.UserInfo.UpdateOneAsync(
                    Builders<DB.UserInfo>.Filter.Eq("_id", Info.DB.Id),
                    Builders<DB.UserInfo>.Update.Set("Config.Image", image)
                ).CAF();
            Info.DB.Config.Image = image;
        }

        public override async ValueTask SetBackgroundImageAsync(string? backgroundImage)
        {
            if (backgroundImage == BackgroundImage)
                return;
            if (!Info.IsGuest)
                await Info.Database.UserInfo.UpdateOneAsync(
                    Builders<DB.UserInfo>.Filter.Eq("_id", Info.DB.Id),
                    Builders<DB.UserInfo>.Update.Set("Config.BackgroundImage", backgroundImage)
                ).CAF();
            Info.DB.Config.BackgroundImage = backgroundImage;
        }

        public override async ValueTask SetLanguageAsync(string language)
        {
            if (language == Language)
                return;
            if (!Info.IsGuest)
                await Info.Database.UserInfo.UpdateOneAsync(
                    Builders<DB.UserInfo>.Filter.Eq("_id", Info.DB.Id),
                    Builders<DB.UserInfo>.Update.Set("Config.Language", language)
                ).CAF();
            Info.DB.Config.Language = language;
        }

        public override async ValueTask SetThemeColorAsync(string themeColor)
        {
            if (themeColor == ThemeColor)
                return;
            if (!Info.IsGuest)
                await Info.Database.UserInfo.UpdateOneAsync(
                    Builders<DB.UserInfo>.Filter.Eq("_id", Info.DB.Id),
                    Builders<DB.UserInfo>.Update.Set("Config.ThemeColor", themeColor)
                ).CAF();
            Info.DB.Config.ThemeColor = themeColor;
        }
    }
}