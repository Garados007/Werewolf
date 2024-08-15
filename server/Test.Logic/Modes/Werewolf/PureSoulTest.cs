using Test.Tools;
using Theme.werewolf;

namespace Test.Logic.Modes.Werewolf;

[TestClass]
public class PureSoulTest
{
    [TestMethod]
    public async Task CheckVisibility()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_PureSoul>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var puresoul = room.GetCharacter<Character_PureSoul>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // verify visibility
        await room.StartGameAsync();
        AreSame(typeof(Character_PureSoul), puresoul.GetSeenRole(room, wolf));
        AreSame(typeof(Character_PureSoul), puresoul.GetSeenRole(room, vill));
        AreSame(typeof(Character_Unknown), wolf.GetSeenRole(room, puresoul));
        AreSame(typeof(Character_Unknown), vill.GetSeenRole(room, puresoul));
    }
}
