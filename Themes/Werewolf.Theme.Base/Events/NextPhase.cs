using System.Text.Json;
using Werewolf.Theme.Chats;
using Werewolf.User;

namespace Werewolf.Theme.Events;

public class NextPhase : GameEvent
{
    public Scene? Phase { get; }

    public NextPhase(Scene? phase)
        => Phase = phase;

    public override ChatServiceMessage? GetLogMessage()
    {
        return Phase is null ? null : new Chats.NextPhaseLog(Phase);
    }

    public override bool CanSendTo(GameRoom game, UserInfo user)
        => true;

    public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
    {
        if (Phase == null)
            writer.WriteNull("phase");
        else
        {
            writer.WriteStartObject("phase"); // phase
            writer.WriteString("lang-id", Phase.LanguageId);
            writer.WriteEndObject();
        }
    }
}
