using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Default.KillInfos
{
    public class VillageKill : KillInfo
    {
        public override string NotificationId => "village-kill";

        public override IEnumerable<string> GetKillFlags(GameRoom game, Role? viewer)
        {
            return Enumerable.Empty<string>();
        }
    }
}
