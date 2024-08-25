using System.Text.Json;

namespace Werewolf.Game.Events;

public class Message : TaggedEvent
{
    public string? Phase { get; set; }

    public string Content { get; set; } = "";

    protected override void Read(JsonElement json)
    {
        if (json.TryGetProperty("phase", out JsonElement element))
            Phase = element.GetString();
        Content = json.GetProperty("message").GetString() ?? "";
    }

    protected override void Write(Utf8JsonWriter writer)
    {
        throw new NotSupportedException();
    }
}
