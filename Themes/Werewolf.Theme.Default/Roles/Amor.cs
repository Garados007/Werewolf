namespace Werewolf.Theme.Default.Roles;

/// <summary>
/// Armor is the role that can make two people in love.
/// </summary>
[Docs.Role]
public class Amor : VillagerBase
{
    public Amor(Theme theme) : base(theme)
    {
    }

    public override Role CreateNew()
    {
        return new Amor(Theme);
    }
}
