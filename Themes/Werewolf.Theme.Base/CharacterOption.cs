namespace Werewolf.Theme;

public class CharacterOption(GameRoom game, Character character)
    : VoteOption("character", ("name", GetName(game, character)))
{
    public Character Character { get; } = character;

    private static string GetName(GameRoom game, Character character)
    {
        var id = game.TryGetId(character);
        return id == null
            ? $"<@unknown>"
            : !game.Users.TryGetValue(id.Value, out var profile)
            ? $"<@{id.Value}>"
            : profile.User.Config.Username;
    }
}
