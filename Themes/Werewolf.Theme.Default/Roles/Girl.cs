namespace Werewolf.Theme.Default.Roles;

public class Girl : VillagerBase
{
    private readonly List<WerwolfBase> seenByWolf
        = new();
    private readonly object lockSeenByWolf = new();

    public void AddSeenByWolf(WerwolfBase wolf)
    {
        lock (lockSeenByWolf)
            seenByWolf.Add(wolf);
        SendRoleInfoChanged();
    }

    public bool IsSeenByWolf(WerwolfBase wolf)
    {
        lock (lockSeenByWolf)
            return seenByWolf.Contains(wolf);
    }

    public Girl(GameMode theme) : base(theme)
    {
    }

    public override string Name => "Mädchen";

    public override Character CreateNew()
    {
        return new Girl(Theme);
    }

    public override Character ViewRole(Character viewer)
    {
        return viewer is WerwolfBase wolf && IsSeenByWolf(wolf)
            ? this
            : base.ViewRole(viewer);
    }
}
