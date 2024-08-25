using Test.Tools;

namespace Test.Logic.Basic.LabelVisibility;

[TestClass]
public class LabelVisibilityTest
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
        IsInstanceOfType<Scene_Scene>(game.Phase.CurrentScene);

        var lbl1 = char1.Labels.GetEffect<Label_VisibleToEveryone>();
        IsNotNull(lbl1);
        IsTrue(lbl1.CanLabelBeSeen(game, char1, char1));
        IsTrue(lbl1.CanLabelBeSeen(game, char1, char2));
        IsTrue(lbl1.CanLabelBeSeen(game, char1, char3));

        var lbl2 = char1.Labels.GetEffect<Label_VisibleToNoone>();
        IsNotNull(lbl2);
        IsFalse(lbl2.CanLabelBeSeen(game, char1, char1));
        IsFalse(lbl2.CanLabelBeSeen(game, char1, char2));
        IsFalse(lbl2.CanLabelBeSeen(game, char1, char3));

        var lbl3 = char1.Labels.GetEffect<Label_VisibleToSelf>();
        IsNotNull(lbl3);
        IsTrue(lbl3.CanLabelBeSeen(game, char1, char1));
        IsFalse(lbl3.CanLabelBeSeen(game, char1, char2));
        IsFalse(lbl3.CanLabelBeSeen(game, char1, char3));

        var lbl4 = char1.Labels.GetEffect<Label_VisibleToSpecific>();
        IsNotNull(lbl4);
        IsFalse(lbl4.CanLabelBeSeen(game, char1, char1));
        IsTrue(lbl4.CanLabelBeSeen(game, char1, char2));
        IsFalse(lbl4.CanLabelBeSeen(game, char1, char3));

        var lbl5 = char1.Labels.GetEffect<Label_VisibleToCustom>();
        IsNotNull(lbl5);
        IsFalse(lbl5.CanLabelBeSeen(game, char1, char1));
        IsTrue(lbl5.CanLabelBeSeen(game, char1, char2));
        IsFalse(lbl5.CanLabelBeSeen(game, char1, char3));

        game.NextScene();
        IsInstanceOfType<Phase_Phase>(game.Phase);
        IsInstanceOfType<Scene_Scene2>(game.Phase.CurrentScene);

        var lbl6 = char1.Labels.GetEffect<Label_VisibleToCustom>();
        IsNotNull(lbl6);
        IsTrue(lbl6.CanLabelBeSeen(game, char1, char1));
        IsFalse(lbl6.CanLabelBeSeen(game, char1, char2));
        IsFalse(lbl6.CanLabelBeSeen(game, char1, char3));

    }
}
