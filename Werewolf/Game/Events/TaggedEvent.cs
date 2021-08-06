using System.Text.Json;
using MaxLib.WebServer.WebSocket;

namespace Werewolf.Game.Events
{
    public abstract class TaggedEvent : EventBase
    {
        public string? Tag { get; set; }

        public sealed override void ReadJsonContent(JsonElement json)
        {
            if (json.TryGetProperty("$tag", out JsonElement tag) && 
                tag.ValueKind == JsonValueKind.String)
                Tag = tag.GetString();
            else Tag = null;

            Read(json);
        }

        protected abstract void Read(JsonElement json);

        protected sealed override void WriteJsonContent(Utf8JsonWriter writer)
        {
            writer.WriteString("$tag", Tag);
            Write(writer);
        }

        protected abstract void Write(Utf8JsonWriter writer);
    }
}