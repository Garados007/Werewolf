using System.Text.Json;
using Werewolf.User;

namespace Werewolf.Theme.Events;

public class SendStats : GameEvent
{
    public override bool CanSendTo(GameRoom game, UserInfo user)
        => true;

    public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
    {
        writer.WriteStartObject("stats");
        foreach (var (id, entry) in game.Users)
        {
            writer.WriteStartObject(id);
            writer.WriteNumber("win-games", entry.User.Stats.WinGames);
            writer.WriteNumber("killed", entry.User.Stats.Killed);
            writer.WriteNumber("loose-games", entry.User.Stats.LooseGames);
            writer.WriteNumber("leader", entry.User.Stats.Leader);
            writer.WriteNumber("level", entry.User.Stats.Level);
            writer.WriteNumber("current-xp", entry.User.Stats.CurrentXp);
            writer.WriteNumber("max-xp", entry.User.Stats.LevelMaxXP);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }
}
