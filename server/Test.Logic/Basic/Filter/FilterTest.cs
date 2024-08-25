using Test.Tools;
using Werewolf.Theme.Labels;

namespace Test.Logic.Basic.Filter;

[TestClass]
public class FilterTest
{
    [TestMethod]
    public async Task TestMethod()
    {
        // setup
        var runner = new Runner<Mode_Mode>()
            .InitChars<Character_User>(3)
            .InitChars<Character_User2>(1)
            .InitChars<Character_User3>(1);
        var game = runner.GameRoom;

        // execute
        await game.StartGameAsync();
        IsInstanceOfType<Phase_Phase>(game.Phase);
        IsInstanceOfType<Scene_Setup>(game.Phase.CurrentScene);
        game.NextScene();
        IsInstanceOfType<Phase_Phase>(game.Phase);
        IsInstanceOfType<Scene_Check>(game.Phase.CurrentScene);

        game.ExpectLabel<IGameRoomLabel, Label_Check_Filter_Has>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Filter_Has2>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Filter_HasNot>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Filter_HasNot2>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Filter_Char1>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Filter_Char2>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Filter_Char3>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Filter_NotChar1>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Filter_NotChar2>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Filter_NotChar3>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Piped_Has>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Piped_Has2>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Piped_HasNot>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Piped_HasNot2>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Piped_Char1>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Piped_Char2>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Piped_Char3>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Piped_NotChar1>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Piped_NotChar2>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Piped_NotChar3>();
        game.ExpectLabel<IGameRoomLabel, Label_Check_Empty>();
    }
}
