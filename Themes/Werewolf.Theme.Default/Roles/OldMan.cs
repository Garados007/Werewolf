using System.Linq;

namespace Werewolf.Theme.Default.Roles
{
    public class OldMan : VillagerBase
    {
        public bool WasKilledByWolvesOneTime { get; set; } = false;

        public bool WasKilledByVillager { get; set; } = false;

        public OldMan(Theme theme) : base(theme)
        {
        }

        public override string Name => "Der Alte";

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
}
