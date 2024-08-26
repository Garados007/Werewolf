using System.Text.Json;

namespace Werewolf.Game.Events;

public class Vote : TaggedEvent
{
    public ulong VotingId { get; set; }

    public int EntryId { get; set; }

    protected override void Read(JsonElement json)
    {
        VotingId = ulong.Parse(json.GetProperty("vid").GetString() ?? "");
        EntryId = int.Parse(json.GetProperty("id").GetString() ?? "");
    }

    protected override void Write(Utf8JsonWriter writer)
    {
        writer.WriteNumber("vid", VotingId);
        writer.WriteNumber("id", EntryId);
    }
}
