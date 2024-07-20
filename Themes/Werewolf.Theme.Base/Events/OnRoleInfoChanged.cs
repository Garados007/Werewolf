using Werewolf.User;
using System.Text.Json;

namespace Werewolf.Theme.Events;

public class OnRoleInfoChanged : GameEvent
{
    public Character Role { get; }

    public uint? ExecutionRound { get; }

    public UserId? Target { get; }

    public OnRoleInfoChanged(Character role, uint? executionRound = null, UserId? target = null)
        => (Role, ExecutionRound, Target) = (role, executionRound, target);

    public override bool CanSendTo(GameRoom game, UserInfo user)
        => Target is null || Target == user.Id;

    public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
    {
        var id = game.TryGetId(Role);
        var ownRole = game.TryGetRole(user.Id);
        var seenRole = id is not null ?
            Character.GetSeenRole(game, ExecutionRound, user, id.Value, Role) : null;
        writer.WriteString("id", id);
        writer.WriteStartArray("tags");
        foreach (var tag in Character.GetSeenTags(game, user, ownRole, Role))
            writer.WriteStringValue(tag);
        writer.WriteEndArray();
        writer.WriteString("role", seenRole?.GetType().Name);
    }
}
