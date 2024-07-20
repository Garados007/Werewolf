using Werewolf.Theme.Effects;
using Werewolf.Theme.Default.Roles;

namespace Werewolf.Theme.Default.Effects;

public class LovedEffect : IRoleEffect
{
    public Character Target { get; }

    public Amor Amor { get; }

    public LovedEffect(Amor amor, Character target)
    {
        Amor = amor;
        Target = target;
    }

    public IEnumerable<string> GetSeenTags(GameRoom game, Character current, Character? viewer)
    {
        if (CanSeeLove(game, current, viewer))
            yield return "loved";
    }

    private bool CanSeeLove(GameRoom game, Character current, Character? viewer)
    {
        // if viewer is current role it can always see its loved state
        // same goes if viewer is not a player
        if (viewer == current || viewer is null)
        {
            return true;
        }
        // if viewer is in love with the current role
        var effect = viewer.Effects.GetEffect<LovedEffect>(x => x.Target == current);
        if (effect is not null)
        {
            return true;
        }
        // if viewer is the responsible amor it can also see this stat
        if (viewer == Amor)
        {
            return true;
        }
        // if viewer is dead and game setting is set
        if (!viewer.Enabled && game.DeadCanSeeAllRoles)
        {
            return true;
        }
        // if current is dead and game setting is set
        if (!current.Enabled && game.AllCanSeeRoleOfDead)
        {
            return true;
        }
        // no one should be able to see
        return false;
    }
}
