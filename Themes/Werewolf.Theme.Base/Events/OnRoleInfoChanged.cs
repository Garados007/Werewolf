using Werewolf.User;
using System.Text.Json;

namespace Werewolf.Theme.Events;

public class OnRoleInfoChanged(Character role, uint? executionRound = null, UserId? target = null) : GameEvent
{
    public Character Role { get; } = role;

    public uint? ExecutionRound { get; } = executionRound;

    public UserId? Target { get; } = target;


    public override bool CanSendTo(GameRoom game, UserInfo user)
        => Target is null || Target == user.Id;

    public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
    {
        var id = game.TryGetId(Role);
        var ownRole = game.TryGetRole(user.Id);
        var seenRole = id is not null ?
            Character.GetSeenRole(game, ExecutionRound, user, id.Value, Role) : null;
        writer.WriteString("id", id);
        writer.WriteBoolean("enabled", Role.Enabled);
        writer.WriteStartArray("tags");
        foreach (var tag in Character.GetSeenTags(game, user, ownRole, Role))
            writer.WriteStringValue(tag);
        writer.WriteEndArray();
        writer.WriteString("role", seenRole is null ? null : game.Theme?.GetCharacterName(seenRole));
    }
}
