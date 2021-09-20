using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Default.KillInfos
{
    public class KilledByLove : KillInfo
    {
        public override IEnumerable<string> GetKillFlags(GameRoom game, RoleKind viewer)
        {
            return Enumerable.Empty<string>();
        }
    }
}
