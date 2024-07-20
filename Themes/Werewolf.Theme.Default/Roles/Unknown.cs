namespace Werewolf.Theme.Default.Roles;

public class Unknown : BaseRole
{
    public Unknown(GameMode theme) : base(theme)
    {
    }

    public override string Name => "unknown";

    public override Character CreateNew()
    {
        return new Unknown(Theme);
    }

    public override bool? IsSameFaction(Character other)
    {
        return null;
    }
}
