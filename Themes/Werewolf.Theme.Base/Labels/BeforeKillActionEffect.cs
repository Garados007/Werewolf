namespace Werewolf.Theme.Labels;

/// <summary>
/// This defines special behavior that needs to be executed before this <see cref="Character" /> is
/// killed.
/// </summary>
public abstract class BeforeKillActionEffect : ICharacterLabel
{
    public abstract void Execute(GameRoom game, Character current);

    public IEnumerable<string> GetSeenTags(GameRoom game, Character current, Character? viewer)
    {
        yield break;
    }

    public void OnAttachCharacter(GameRoom game, Character target)
    {
    }

    public void OnDetachCharacter(GameRoom game, Character target)
    {
    }

}
