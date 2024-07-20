namespace Werewolf.Theme.Default;

public abstract class WerwolfBase : BaseRole
{
    private readonly List<Roles.Girl> seenByGirl = new();
    private readonly object lockSeenByGirl = new();

    public void AddSeenByGirl(Roles.Girl girl)
    {
        lock (lockSeenByGirl)
            seenByGirl.Add(girl);
        SendRoleInfoChanged();
    }

    public bool IsSeenByGirl(Roles.Girl girl)
    {
        lock (lockSeenByGirl)
            return seenByGirl.Contains(girl);
    }

    protected WerwolfBase(GameMode theme) : base(theme)
    {
    }

    public override bool? IsSameFaction(Character other)
    {
        return other is WerwolfBase
            ? true
            : null;
    }

    public override Character ViewRole(Character viewer)
    {
        return viewer is WerwolfBase || (viewer is Roles.Girl girl && IsSeenByGirl(girl))
            ? new Roles.Werwolf(Theme)
            : base.ViewRole(viewer);
    }
}
