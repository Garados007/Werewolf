using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Default.KillInfos
{
    public class ScapeGoatKilled : KillInfo
    {
        public override string NotificationId => "scapegoat-kill";

        public override IEnumerable<string> GetKillFlags(GameRoom game, RoleKind viewer)
        {
            return Enumerable.Empty<string>();
        }
    }
}
