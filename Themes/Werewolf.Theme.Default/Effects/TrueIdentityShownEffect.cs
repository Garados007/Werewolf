using Werewolf.Theme.Labels;

namespace Werewolf.Theme.Default.Effects;

public class TrueIdentityShownEffect : ICharacterLabel
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
