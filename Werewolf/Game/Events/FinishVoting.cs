using System.Text.Json;

namespace Werewolf.Game.Events;

public class FinishVoting : TaggedEvent
{
    public ulong VotingId { get; set; }

    protected override void Read(JsonElement json)
    {
        VotingId = json.GetProperty("vid").GetUInt64();
    }

    protected override void Write(Utf8JsonWriter writer)
    {
        writer.WriteNumber("vid", VotingId);
    }
}
