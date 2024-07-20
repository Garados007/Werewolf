namespace Werewolf.Theme.Effects;

public interface IRoleEffect : IEffect
{
    IEnumerable<string> GetSeenTags(GameRoom game, Character current, Character? viewer);
}
