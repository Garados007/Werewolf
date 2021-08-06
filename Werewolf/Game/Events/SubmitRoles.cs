using System.Text.Json;
using System.Collections.Generic;

namespace Werewolf.Game.Events
{
    public class SubmitRoles : TaggedEvent
    {
        public override string TypeName => "submit-roles";

        public Dictionary<string, List<string>> Roles { get; }
            = new Dictionary<string, List<string>>();

        protected override void Read(JsonElement json)
        {
            Roles.Clear();
            foreach (var entry in json.GetProperty("roles").EnumerateObject())
            {
                var list = new List<string>();
                Roles.Add(entry.Name, list);
                foreach (var item in entry.Value.EnumerateArray())
                    list.Add(item.GetString() ?? "");
            }
        }

        protected override void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject("roles");
            foreach (var (key, values) in Roles)
            {
                writer.WriteStartArray(key);
                foreach (var value in values)
                    writer.WriteStringValue(value);
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
    }
}