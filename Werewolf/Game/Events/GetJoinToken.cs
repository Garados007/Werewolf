using System.Text.Json;

namespace Werewolf.Game.Events;

public class GetJoinToken : TaggedEvent
{
    public string Token { get; set; } = "";

    public DateTime AliveUntil { get; set; }

    protected override void Read(JsonElement json)
    {
        throw new NotSupportedException();
    }

    protected override void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("token", Token);
        writer.WriteString("alive-until", AliveUntil);
    }
}
