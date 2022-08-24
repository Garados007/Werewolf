namespace Werewolf.Theme.Default.Roles;

[Docs.Role]
public class Angel : VillagerBase
{
    public bool MissedFirstRound { get; set; }

    public Angel(Theme theme) : base(theme)
    {
    }

    public override Role CreateNew()
    {
        return new Angel(Theme);
    }
}
