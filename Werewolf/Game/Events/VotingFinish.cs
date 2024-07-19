namespace Werewolf.Game.Events;

public class VotingFinish : TaggedEvent
{
    public ulong VotingId { get; set; }

    protected override void Read(JsonElement json)
    {
        VotingId = ulong.Parse(json.GetProperty("vid").GetString() ?? "");
    }

    protected override void Write(Utf8JsonWriter writer)
    {
        writer.WriteNumber("vid", VotingId);
    }
}
