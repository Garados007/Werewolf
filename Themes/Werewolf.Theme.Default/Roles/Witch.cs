namespace Werewolf.Theme.Default.Roles;

public class Witch : VillagerBase
{
    public bool UsedLivePotion { get; set; }

    public bool UsedDeathPotion { get; set; }

    public Witch(GameMode theme) : base(theme)
    {
    }


    public override string Name => "Hexe";

    public override Character CreateNew()
    {
        return new Witch(Theme);
    }

    public override Character ViewRole(Character viewer)
    {
        return viewer is Witch
            ? this
            : base.ViewRole(viewer);
    }
}
