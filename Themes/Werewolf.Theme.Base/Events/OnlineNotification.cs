using System.Text.Json;
using Werewolf.User;

namespace Werewolf.Theme.Events;

public class OnlineNotification : GameEvent
{
    public GameUserEntry UserEntry { get; }

    public OnlineNotification(GameUserEntry userEntry)
        => UserEntry = userEntry;

    public override bool CanSendTo(GameRoom game, UserInfo user)
        => true;

    public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
    {
        writer.WriteString("user", UserEntry.User.Id);
        writer.WriteBoolean("online", UserEntry.IsOnline);
        writer.WriteNumber("counter", UserEntry.ConnectionChanged);
        writer.WriteString("last-changed", UserEntry.LastConnectionUpdate);
    }
}
