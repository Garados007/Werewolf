using System.Collections.Generic;

namespace Werewolf.Theme.Default.KillInfos
{
    public class KilledByWithDeathPotion : KillInfo
    {
        public override string NotificationId => "night-kills";

        [Docs.Tag("witch-death-potion", "This player got the death potion from the witch")]
        public override IEnumerable<string> GetKillFlags(GameRoom game, Role? viewer)
        {
            if (viewer is null or Roles.Witch)
                yield return "witch-death-potion";
        }
    }
}
