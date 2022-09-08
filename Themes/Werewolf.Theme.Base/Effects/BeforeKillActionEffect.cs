namespace Werewolf.Theme.Effects;

/// <summary>
/// This defines special behavior that needs to be executed before this <see cref="Role" /> is
/// killed. 
/// </summary>
public abstract class BeforeKillActionEffect : IRoleEffect
{
    public abstract void Execute(GameRoom game, Role current);

    public IEnumerable<string> GetSeenTags(GameRoom game, Role current, Role? viewer)
    {
        yield break;
    }
}