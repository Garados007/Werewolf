using System.Text.Json;

namespace Werewolf.User;

public abstract class UserInfo
{
    public abstract UserId Id { get; }

    public abstract string? OAuthId { get; }

    public bool IsGuest => OAuthId is null || OAuthId.Length == 0;

    public abstract UserConfig Config { get; }

    public abstract UserStats Stats { get; }

    public virtual void WriteContent(Utf8JsonWriter writer)
    {
        writer.WriteString("id", Id);
        writer.WriteString("name", Config.Username);
        writer.WriteString("img", Config.Image);
        writer.WriteBoolean("is-guest", IsGuest);
        writer.WriteStartObject("stats");
        writer.WriteNumber("win-games", Stats.WinGames);
        writer.WriteNumber("killed", Stats.Killed);
        writer.WriteNumber("loose-games", Stats.LooseGames);
        writer.WriteNumber("leader", Stats.Leader);
        writer.WriteNumber("level", Stats.Level);
        writer.WriteNumber("current-xp", Stats.CurrentXp);
        writer.WriteNumber("max-xp", Stats.LevelMaxXP);
        writer.WriteEndObject();

    }
}
