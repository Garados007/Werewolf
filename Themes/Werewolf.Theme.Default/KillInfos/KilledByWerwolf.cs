using System.Collections.Generic;

namespace Werewolf.Theme.Default.KillInfos
{
    public class KilledByWerwolf : KillInfo
    {
        public override string NotificationId => "night-kills";

        public override IEnumerable<string> GetKillFlags(GameRoom game, Role? viewer)
        {
            if (viewer == null || viewer is Roles.Werwolf || viewer is Roles.Witch)
                yield return "werwolf-select";
        }
    }
}
