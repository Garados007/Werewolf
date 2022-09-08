namespace Werewolf.Theme.Effects;

public interface IRoleEffect : IEffect
{
    IEnumerable<string> GetSeenTags(GameRoom game, Role current, Role? viewer);
}
