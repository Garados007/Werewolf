using Werewolf.Theme.Effects;

namespace Werewolf.Theme.Default.Effects;

public class TrueIdentityShownEffect : IRoleEffect
{
    public Character Viewer { get; }

    public TrueIdentityShownEffect(Character viewer)
    {
        Viewer = viewer;
    }

    public IEnumerable<string> GetSeenTags(GameRoom game, Character current, Character? viewer)
    {
        return Enumerable.Empty<string>();
    }
}
