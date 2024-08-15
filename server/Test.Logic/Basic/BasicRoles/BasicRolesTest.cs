using Test.Tools;
using Werewolf.Theme.Labels;

namespace Test.Logic.Basic.BasicRoles;

[TestClass]
public class BasicRolesTest
{
    [TestMethod]
    public async Task TestMethod()
    {
        // setup
        var runner = new Runner<Mode_BasicRoles>()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Wolf>(1);
        var game = runner.GameRoom;
        game.AutoFinishRounds = true;
        var vill1 = game.GetCharacter<Character_Villager>(0);
        var wolf1 = game.GetCharacter<Character_Wolf>(0);

        // execute
        await game.StartGameAsync();
        IsNotNull(game.Phase);
        IsInstanceOfType<Phase_BasicPhase>(game.Phase);
        IsInstanceOfType<Scene_BasicScene>(game.Phase.CurrentScene);

        // let them vote each other
        var voting = game.ExpectVoting<Voting_BasicVoting>();
        AreEqual(null, voting.Vote(game, vill1, wolf1));
        AreEqual(null, voting.Vote(game, wolf1, vill1));
        var previousPhase = game.Phase;
        var previousScene = game.Phase.CurrentScene;
        voting.FinishVoting(game);
        AreSame(previousPhase, game.Phase);
        AreSame(previousScene, game.Phase.CurrentScene);

        // let the vote again but decide for one
        var old = voting;
        voting = game.ExpectVoting<Voting_BasicVoting>();
        AreNotSame(old, voting);
        AreEqual(null, voting.Vote(game, vill1, wolf1));
        AreEqual(null, voting.Vote(game, wolf1, wolf1));
        voting.FinishVoting(game);
        AreNotSame(previousPhase, game.Phase);
        AreNotSame(previousScene, game.Phase.CurrentScene);
        wolf1.ExpectLabel<ICharacterLabel, Label_TestLabel>();
        vill1.ExpectNoLabel<ICharacterLabel, Label_TestLabel>();

        // and now for the other one
        old = voting;
        voting = game.ExpectVoting<Voting_BasicVoting>();
        AreNotSame(old, voting);
        AreEqual(null, voting.Vote(game, vill1, vill1));
        AreEqual(null, voting.Vote(game, wolf1, vill1));
        voting.FinishVoting(game);
        AreNotSame(previousPhase, game.Phase);
        AreNotSame(previousScene, game.Phase.CurrentScene);
        wolf1.ExpectLabel<ICharacterLabel, Label_TestLabel>();
        vill1.ExpectLabel<ICharacterLabel, Label_TestLabel>();
    }
}
