using System.Text.Json;
using Werewolf.User;

namespace Werewolf.Theme.Events
{
    public class SetUserConfig : GameEvent
    {
        public UserInfo User { get; }

        public SetUserConfig(UserInfo user)
            => User = user;

        public override bool CanSendTo(GameRoom game, UserInfo user)
        {
            return user.Id == User.Id;
        }

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            var userConfig = game.Theme?.Users.GetCachedUser(user.Id);
            if (userConfig != null)
            {
                writer.WriteStartObject("user-config");
                writer.WriteString("theme", userConfig.Config.ThemeColor ?? "#333333");
                writer.WriteString("background", userConfig.Config.BackgroundImage ?? "");
                writer.WriteString("language", string.IsNullOrEmpty(userConfig.Config.Language) ? "de" : userConfig.Config.Language);
                writer.WriteEndObject();
            }
            else writer.WriteNull("user-config");
        }
    }
}
