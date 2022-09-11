using Werewolf.Theme.Effects;

namespace Werewolf.Theme.Default.Effects.KillInfos
{
    public class KilledByWithDeathPotion : KillInfoEffect
    {
        public override string NotificationId => "night-kills";

        public override IEnumerable<string> GetSeenTags(GameRoom game, Role current, Role? viewer)
        {
            foreach (var tag in base.GetSeenTags(game, current, viewer))
                yield return tag;
            if (viewer is null or Roles.Witch)
                yield return "witch-death-potion";
        }
    }
}
