namespace Werewolf.Theme.Effects;

/// <summary>
/// This defines special behavior that needs to be executed before this <see cref="Character" /> is
/// killed.
/// </summary>
public abstract class BeforeKillActionEffect : IRoleEffect
{
    public abstract void Execute(GameRoom game, Character current);

    public IEnumerable<string> GetSeenTags(GameRoom game, Character current, Character? viewer)
    {
        yield break;
    }
}
