namespace Werewolf.Theme.Labels;

public interface ICharacterLabel : ILabel
{
    /// <summary>
    /// Checks whether this label can be seen at all and if so returns a list of displayed tags.
    /// This usually contains only the name of this label.
    /// </summary>
    /// <param name="game">The current game</param>
    /// <param name="current">The current character with this label</param>
    /// <param name="viewer">The viewing character. It is null if the user has no character associated.</param>
    /// <returns></returns>
    IEnumerable<string> GetSeenTags(GameRoom game, Character current, Character? viewer);

    void OnAttachCharacter(GameRoom game, Character target);

    void OnDetachCharacter(GameRoom game, Character target);
}
