namespace Werewolf.Users
{
    public class DbUserConfig
    {
        public string Username { get; set; } = "";

        public string Image { get; set; } = "";

        public string ThemeColor { get; set; } = "#ffffff";

        public string BackgroundImage { get; set; } = "";

        public string Language { get; set; } = "de";

        public DbUserConfig() { }

        public DbUserConfig(Api.UserConfig api)
        {
            Username = api.Username;
            Image = api.Image;
            ThemeColor = api.ThemeColor;
            BackgroundImage = api.BackgroundImage;
            Language = api.Language;
        }

        public Api.UserConfig ToApi()
        {
            return new Api.UserConfig
            {
                Username = Username,
                Image = Image,
                ThemeColor = ThemeColor,
                BackgroundImage = BackgroundImage,
                Language = Language,
            };
        }
    }
}