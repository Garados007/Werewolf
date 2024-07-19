namespace Werewolf.Game.Events;

public class SetGameConfig : TaggedEvent
{
    public UserId? Leader { get; set; }

    public Dictionary<string, int>? RoleConfig { get; set; }

    public bool? LeaderIsPlayer { get; set; }

    public bool? DeadCanSeeAllRoles { get; set; }

    public bool? AllCanSeeRoleOfDead { get; set; }

    public bool? AutostartVotings { get; set; }

    public bool? AutoFinishVotings { get; set; }

    public bool? UseVotingTimeouts { get; set; }

    public bool? AutoFinishRounds { get; set; }

    public string? Theme { get; set; }

    public string? ThemeLang { get; set; }

    protected override void Read(JsonElement json)
    {
        if (json.TryGetProperty("leader", out JsonElement element))
            Leader = new UserId(element.GetString() ?? throw new InvalidOperationException());
        if (json.TryGetProperty("config", out element))
        {
            RoleConfig = new Dictionary<string, int>();
            foreach (var entry in element.EnumerateObject())
                RoleConfig.Add(entry.Name, entry.Value.GetInt32());
        }
        if (json.TryGetProperty("leader-is-player", out element))
            LeaderIsPlayer = element.GetBoolean();
        if (json.TryGetProperty("dead-can-see-all-roles", out element))
            DeadCanSeeAllRoles = element.GetBoolean();
        if (json.TryGetProperty("all-can-see-role-of-dead", out element))
            AllCanSeeRoleOfDead = element.GetBoolean();
        if (json.TryGetProperty("autostart-votings", out element))
            AutostartVotings = element.GetBoolean();
        if (json.TryGetProperty("autofinish-votings", out element))
            AutoFinishVotings = element.GetBoolean();
        if (json.TryGetProperty("voting-timeout", out element))
            UseVotingTimeouts = element.GetBoolean();
        if (json.TryGetProperty("autofinish-rounds", out element))
            AutoFinishRounds = element.GetBoolean();
        if (json.TryGetProperty("theme-impl", out element))
            Theme = element.GetString();
        if (json.TryGetProperty("theme-lang", out element))
            ThemeLang = element.GetString();
    }

    protected override void Write(Utf8JsonWriter writer)
    {
        throw new NotSupportedException();
    }
}
