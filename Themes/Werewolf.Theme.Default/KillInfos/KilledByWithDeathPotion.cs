using System.Collections.Generic;

namespace Werewolf.Theme.Default.KillInfos
{
    public class KilledByWithDeathPotion : KillInfo
    {
        public override string NotificationId => "night-kills";

        public override IEnumerable<string> GetKillFlags(GameRoom game, RoleKind viewer)
        {
            if (viewer.IsLeaderOrRole<Roles.Witch>())
                yield return "witch-death-potion";
        }
    }
}
