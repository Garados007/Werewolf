using System.Text.Json;
using Werewolf.User;

namespace Werewolf.Theme.Events;

public class SendStage : GameEvent
{
    public Phase Stage { get; }

    public SendStage(Phase stage)
        => Stage = stage;

    public override bool CanSendTo(GameRoom game, UserInfo user)
        => true;

    public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
    {
        writer.WriteString("lang-id", (string)Stage.LanguageId);
        writer.WriteString("background-id", Stage.BackgroundId);
        writer.WriteString("theme", Stage.ColorTheme);
    }
}
