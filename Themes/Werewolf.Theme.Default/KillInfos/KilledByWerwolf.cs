using System.Collections.Generic;

namespace Werewolf.Theme.Default.KillInfos
{
    public class KilledByWerwolf : KillInfo
    {
        public override string NotificationId => "night-kills";

        [Docs.Tag("werwolf-select", "This player was selected by the werewolves.")]
        public override IEnumerable<string> GetKillFlags(GameRoom game, Role? viewer)
        {
            if (viewer is null or Roles.Werwolf or Roles.Witch)
                yield return "werwolf-select";
        }
    }
}
