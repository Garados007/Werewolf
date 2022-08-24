namespace Werewolf.Theme.Default.Roles;

[Docs.Role]
public class Healer : VillagerBase
{
    public Healer(Theme theme) : base(theme)
    {
    }

    public override Role CreateNew()
    {
        return new Healer(Theme);
    }
}

