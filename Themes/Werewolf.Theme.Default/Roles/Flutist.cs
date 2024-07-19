namespace Werewolf.Theme.Default.Roles;

public class Flutist : BaseRole
{
    public Flutist(GameMode theme)
        : base(theme)
    {
    }

    public override string Name => "Der Flötenspieler";

    public override Role CreateNew()
        => new Flutist(Theme);

    public override bool? IsSameFaction(Role other)
    {
        return other is Flutist;
    }
}
