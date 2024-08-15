using Test.Tools;
using Werewolf.Theme.Labels;

namespace Test.Logic.Basic.Enabled;

[TestClass]
public class EnabledTest
{
    [TestMethod]
    public async Task TestMethod()
    {
        // setup
        var runner = new Runner<Mode_Mode>()
            .InitChars<Character_User>(3);
        var game = runner.GameRoom;
        var char1 = game.GetCharacter<Character_User>(0);
        var char2 = game.GetCharacter<Character_User>(1);
        var char3 = game.GetCharacter<Character_User>(2);

        // execute
        await game.StartGameAsync();
        IsInstanceOfType<Phase_Phase>(game.Phase);
        IsInstanceOfType<Scene_Scene1>(game.Phase.CurrentScene);
        IsFalse(char1.Enabled);
        IsTrue(char2.Enabled);
        IsTrue(char3.Enabled);

        game.NextScene();
        IsInstanceOfType<Phase_Phase>(game.Phase);
        IsInstanceOfType<Scene_Scene2>(game.Phase.CurrentScene);
        IsTrue(char1.Enabled);
        IsTrue(char2.Enabled);
        IsTrue(char3.Enabled);
        char1.ExpectNoLabel<ICharacterLabel, Label_Marker1>();
        char2.ExpectLabel<ICharacterLabel, Label_Marker1>();
        char3.ExpectLabel<ICharacterLabel, Label_Marker1>();
        char1.ExpectLabel<ICharacterLabel, Label_Marker2>();
        char2.ExpectLabel<ICharacterLabel, Label_Marker2>();
        char3.ExpectLabel<ICharacterLabel, Label_Marker2>();
    }
}
