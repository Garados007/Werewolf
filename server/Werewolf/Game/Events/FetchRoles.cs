using System.Text.Json;

namespace Werewolf.Game.Events;

public class FetchRoles : TaggedEvent
{
    public override string TypeName => "fetch-roles";

    protected override void Read(JsonElement json)
    {
    }

    protected override void Write(Utf8JsonWriter writer)
    {
    }
}
