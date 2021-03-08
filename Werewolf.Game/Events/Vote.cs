using System.Text.Json;

namespace Werewolf.Game.Events
{
    public class Vote : TaggedEvent
    {
        public ulong VotingId { get; set; }

        public int EntryId { get; set; }

        protected override void Read(JsonElement json)
        {
            VotingId = json.GetProperty("vid").GetUInt64();
            EntryId = json.GetProperty("id").GetInt32();
        }

        protected override void Write(Utf8JsonWriter writer)
        {
            writer.WriteNumber("vid", VotingId);
            writer.WriteNumber("id", EntryId);
        }
    }
}