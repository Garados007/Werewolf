namespace Werewolf.Theme.Default.Roles;

public class Werwolf : WerwolfBase
{
    public Werwolf(GameMode theme) : base(theme)
    {
    }

    public override string Name => "Werwolf";

    public override Character CreateNew()
    {
        return new Werwolf(Theme);
    }
}
