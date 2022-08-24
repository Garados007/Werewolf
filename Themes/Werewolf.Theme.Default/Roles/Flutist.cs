namespace Werewolf.Theme.Default.Roles;

[Docs.Role]
public class Flutist : BaseRole
{
    public Flutist(Theme theme)
        : base(theme)
    {
    }

    public override Role CreateNew()
        => new Flutist(Theme);

    public override bool? IsSameFaction(Role other)
    {
        return other is Flutist;
    }
}
