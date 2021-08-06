using System.Text.Json;
using Werewolf.User;
using System;

namespace Werewolf.Game.Events
{
    public class KickUser : TaggedEvent
    {
        public UserId User { get; set; } = new UserId();

        protected override void Read(JsonElement json)
        {
            User = new UserId(json.GetProperty("user").GetString() ?? 
                throw new InvalidOperationException());
        }

        protected override void Write(Utf8JsonWriter writer)
        {
            throw new NotSupportedException();
        }
    }
}