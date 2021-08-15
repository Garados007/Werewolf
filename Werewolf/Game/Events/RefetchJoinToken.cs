using System.Text.Json;

namespace Werewolf.Game.Events
{
    public class RefetchJoinToken : TaggedEvent
    {
        protected override void Read(JsonElement json)
        {
        }

        protected override void Write(Utf8JsonWriter writer)
        {
        }
    }
}