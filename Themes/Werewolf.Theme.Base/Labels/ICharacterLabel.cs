namespace Werewolf.Theme.Labels;

public interface ICharacterLabel : ILabel
{
    /// <summary>
    /// A list of characters that can see this label regardless of the <see
    /// cref="GetSeenTags(GameRoom, Character, Character?)"/> result.
    /// </summary>
    List<Character> Visible { get; }

    string Name { get; }

    /// <summary>
    /// Checks whether this label can be seen at all.
    /// </summary>
    /// <param name="game">The current game</param>
    /// <param name="current">The current character with this label</param>
    /// <param name="viewer">The viewing character. It is null if the user has no character associated.</param>
    /// <returns></returns>
    bool CanLabelBeSeen(GameRoom game, Character current, Character? viewer);

    void OnAttachCharacter(GameRoom game, ICharacterLabel label, Character target);

    void OnDetachCharacter(GameRoom game, ICharacterLabel label, Character target);
}
