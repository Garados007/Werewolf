using Test.Tools;

namespace Test.Logic.Basic.RandomStuff;

[TestClass]
public class RandomStuffTest
{
    [TestMethod]
    public async Task TestMethod()
    {
        // setup
        Werewolf.Theme.Tools.SetSeed(1337);
        var runner = new Runner<Mode_Random>()
            .InitChars<Character_User>(10);
        var game = runner.GameRoom;

        // execute
        await game.StartGameAsync();
        IsNotNull(game.Phase);
        IsNotNull(game.Phase.CurrentScene);
        CollectionAssert.AreEquivalent(
            new List<Character_User>
            {
                game.GetCharacter<Character_User>(2),
                game.GetCharacter<Character_User>(3),
                game.GetCharacter<Character_User>(4),
                game.GetCharacter<Character_User>(5),
                game.GetCharacter<Character_User>(8),
            },
            game.AllCharacters
                .Where(x => x.Labels.GetEffect<Label_SelGroup>() is not null)
                .ToList()
        );
        CollectionAssert.AreEquivalent(
            new List<Character_User>
            {
                game.GetCharacter<Character_User>(4),
            },
            game.AllCharacters
                .Where(x => x.Labels.GetEffect<Label_SelSingle>() is not null)
                .ToList()
        );
    }
}
