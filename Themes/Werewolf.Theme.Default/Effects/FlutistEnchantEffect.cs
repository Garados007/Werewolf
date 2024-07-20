using Werewolf.Theme.Effects;
using Werewolf.Theme.Default.Roles;

namespace Werewolf.Theme.Default.Effects;

public class FlutistEnchantEffect : IRoleEffect
{
    public Flutist Flutist { get; }

    public FlutistEnchantEffect(Flutist flutist)
    {
        Flutist = flutist;
    }

    public IEnumerable<string> GetSeenTags(GameRoom game, Character current, Character? viewer)
    {
        if (CanSeeEnchant(game, current, viewer))
            yield return "enchant-flutist";
    }

    private bool CanSeeEnchant(GameRoom game, Character current, Character? viewer)
    {
        // if viewer is current role it can always see its enchant state
        // same goes if viewer is not a player
        if (viewer == current || viewer is null)
            return true;
        // if viewer is the responsible flutist
        if (viewer is Flutist)
            return true;
        // if viewer is dead and game setting is set
        if (!viewer.Enabled && game.DeadCanSeeAllRoles)
            return true;
        // if current is dead and game setting is set
        if (!current.Enabled && game.AllCanSeeRoleOfDead)
            return true;
        // if viewer is enchanted by the same flutist
        if (viewer.Effects.GetEffect<FlutistEnchantEffect>(x => x.Flutist == Flutist) is not null)
            return true;
        // no one should be able to see
        return false;
    }
}
