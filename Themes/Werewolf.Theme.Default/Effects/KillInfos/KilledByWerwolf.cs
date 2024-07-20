using Werewolf.Theme.Labels;

namespace Werewolf.Theme.Default.Effects.KillInfos;

public class KilledByWerwolf : KillInfoEffect
{
    public override string NotificationId => "night-kills";

    public override IEnumerable<string> GetSeenTags(GameRoom game, Character current, Character? viewer)
    {
        foreach (var tag in base.GetSeenTags(game, current, viewer))
            yield return tag;
        if (viewer is null or Roles.Werwolf or Roles.Witch)
            yield return "werwolf-select";
    }
}
