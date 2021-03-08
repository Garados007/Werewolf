using System;
using System.Text.Json;

namespace Werewolf.Game.Events
{
    public class ErrorMessage : TaggedEvent
    {
        public string Message { get; set; } = "";

        protected override void Read(JsonElement json)
        {
            Message = json.GetProperty("error").GetString() ?? 
                throw new InvalidOperationException();
        }

        protected override void Write(Utf8JsonWriter writer)
        {
            writer.WriteString("error", Message);
        }
    }
}