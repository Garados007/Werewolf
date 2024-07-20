namespace Werewolf.Theme.Default.Roles;

public class Idiot : VillagerBase
{
    private bool isRevealed;
    public bool IsRevealed
    {
        get => isRevealed;
        set
        {
            isRevealed = value;
            SendRoleInfoChanged();
        }
    }

    public bool WasMajor { get; set; }

    public Idiot(GameMode theme) : base(theme)
    {
    }

    public override Character ViewRole(Character viewer)
    {
        return IsRevealed
            ? this
            : base.ViewRole(viewer);
    }

    public override string Name => "Dorfdepp";

    public override Character CreateNew()
    {
        return new Idiot(Theme);
    }
}
