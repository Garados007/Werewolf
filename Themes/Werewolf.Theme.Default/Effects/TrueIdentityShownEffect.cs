using Werewolf.Theme.Effects;

namespace Werewolf.Theme.Default.Effects;

public class TrueIdentityShownEffect : IRoleEffect
{
    public Role Viewer { get; }

    public TrueIdentityShownEffect(Role viewer)
    {
        Viewer = viewer;
    }

    public IEnumerable<string> GetSeenTags(GameRoom game, Role current, Role? viewer)
    {
        return Enumerable.Empty<string>();
    }
}