using System.Text.Json;
using MaxLib.WebServer.WebSocket;

namespace Werewolf.Game.Events;

public class EnterMaintenance : EventBase
{
    public DateTime ForcedShutdown { get; set; }

    public string? Reason { get; set; }

    public override void ReadJsonContent(JsonElement json)
    {
        if (json.TryGetProperty("reason", out JsonElement node))
            Reason = node.GetString();
        ForcedShutdown = json.GetProperty("forced-shutdown").GetDateTime();
    }

    protected override void WriteJsonContent(Utf8JsonWriter writer)
    {
        writer.WriteString("reason", Reason);
        writer.WriteString("forced-shutdown", ForcedShutdown);
    }
}
