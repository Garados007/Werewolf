namespace Werewolf.Game.Events;

public class SetUserConfig : TaggedEvent
{
    public string? Theme { get; set; }

    public string? BackgroundImage { get; set; }

    public string? Language { get; set; }

    protected override void Read(JsonElement json)
    {
        if (json.TryGetProperty("theme", out JsonElement element))
            Theme = element.GetString();
        if (json.TryGetProperty("background", out element))
            BackgroundImage = element.GetString();
        if (json.TryGetProperty("language", out element))
            Language = element.GetString();
    }

    protected override void Write(Utf8JsonWriter writer)
    {
        throw new NotSupportedException();
    }
}
