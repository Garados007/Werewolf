using System.Text.Json;
using Werewolf.Users.Api;

namespace Werewolf.Theme.Events
{
    public class SendStage : GameEvent
    {
        public Stage Stage { get; }

        public SendStage(Stage stage)
            => Stage = stage;

        public override bool CanSendTo(GameRoom game, UserInfo user)
            => true;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            writer.WriteString("lang-id", Stage.LanguageId);
            writer.WriteString("background-id", Stage.BackgroundId);
            writer.WriteString("theme", Stage.ColorTheme);
        }
    }
}
