using System.Collections.Generic;

namespace Werewolf.Theme.Default.KillInfos
{
    public class KilledByWerwolf : KillInfo
    {
        public override string NotificationId => "night-kills";

        public override IEnumerable<string> GetKillFlags(GameRoom game, RoleKind viewer)
        {
            if (viewer.IsLeaderOrRole<Roles.Werwolf, Roles.Witch>())
                yield return "werwolf-select";
        }
    }
}
