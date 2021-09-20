using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Default.KillInfos
{
    public class KilledByMajor : KillInfo
    {
        public override string NotificationId => "killed-by-major";

        public override IEnumerable<string> GetKillFlags(GameRoom game, RoleKind viewer)
        {
            return Enumerable.Empty<string>();
        }
    }
}
