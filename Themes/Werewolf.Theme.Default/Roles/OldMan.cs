namespace Werewolf.Theme.Default.Roles;

[Docs.Role]
public class OldMan : VillagerBase
{
    public bool WasKilledByWolvesOneTime { get; set; }

    public bool WasKilledByVillager { get; set; }

    public OldMan(Theme theme) : base(theme)
    {
    }

    public override Role CreateNew()
        => new OldMan(Theme);

    public override void ChangeToAboutToKill(GameRoom game)
    {
        base.ChangeToAboutToKill(game);
        var idiots = game.AliveRoles
            .Where(x => x is Idiot idiot && idiot.IsRevealed);
        foreach (var idiot in idiots)
            idiot.SetKill(game, new KillInfos.OldManKillsIdiot());
    }
}
