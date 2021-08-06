using System.Text.Json;

namespace Werewolf.Game.Events
{
    public class Success : TaggedEvent
    {
        protected override void Read(JsonElement json)
        {
        }

        protected override void Write(Utf8JsonWriter writer)
        {
        }
    }
}