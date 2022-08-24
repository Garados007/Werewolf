namespace Werewolf.Theme.Default.Roles;

[Docs.Role]
public class Unknown : BaseRole
{
    public Unknown(Theme theme) : base(theme)
    {
    }

    public override Role CreateNew()
    {
        return new Unknown(Theme);
    }

    public override bool? IsSameFaction(Role other)
    {
        return null;
    }
}
