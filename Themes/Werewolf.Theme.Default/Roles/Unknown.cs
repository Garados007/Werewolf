namespace Werewolf.Theme.Default.Roles;

public class Unknown : BaseRole
{
    public Unknown(GameMode theme) : base(theme)
    {
    }

    public override string Name => "unknown";

    public override Role CreateNew()
    {
        return new Unknown(Theme);
    }

    public override bool? IsSameFaction(Role other)
    {
        return null;
    }
}
