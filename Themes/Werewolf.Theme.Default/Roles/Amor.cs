namespace Werewolf.Theme.Default.Roles;

public class Amor : VillagerBase
{
    public Amor(GameMode theme) : base(theme)
    {
    }

    public override string Name => "Amor";

    public override Character CreateNew()
    {
        return new Amor(Theme);
    }
}
