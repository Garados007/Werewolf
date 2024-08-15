using Werewolf.User;
using System.Text.Json;

namespace Werewolf.Theme.Events;

public class CoreNotification(string? sequence, string message) : GameEvent
{
    public string? Sequence { get; } = sequence;

    public string Message { get; } = message;

    public override bool CanSendTo(GameRoom game, UserInfo user)
    {
        return true;
    }

    public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
    {
        writer.WriteString("sequence", Sequence);
        writer.WriteString("message", Message);
    }
}
