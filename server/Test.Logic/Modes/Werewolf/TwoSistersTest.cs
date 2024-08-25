using Test.Tools;
using Theme.werewolf;

namespace Test.Logic.Modes.Werewolf;

[TestClass]
public class TwoSistersTest
{
    [TestMethod]
    public async Task TwoSistersDiscussion()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_TwoSisters>(2)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var sister1 = room.GetCharacter<Character_TwoSisters>(0);
        var sister2 = room.GetCharacter<Character_TwoSisters>(1);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_TwoSisters>(room.Phase?.CurrentScene);

        // vote for continue
        {
            AreEqual(typeof(Character_TwoSisters), sister1.GetSeenRole(room, sister2));
            var voting = room.ExpectVoting<Voting_TwoSisters>();
            IsNull(voting.Vote<Option_Continue>(room, sister1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        }
    }

    [TestMethod]
    public async Task TwoSistersLostAbilityAfterOldManDiesFromVillage()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_TwoSisters>(2)
            .InitChars<Character_OldMan>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var sister1 = room.GetCharacter<Character_TwoSisters>(0);
        var sister2 = room.GetCharacter<Character_TwoSisters>(1);
        var oldman = room.GetCharacter<Character_OldMan>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired oneselect spy
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_TwoSisters>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // old man dies
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, oldman));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }

        // two sister discussion phase is now lost
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
    }

    [TestMethod]
    public async Task CheckVisibility()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_TwoSisters>(2)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var sister1 = room.GetCharacter<Character_TwoSisters>(0);
        var sister2 = room.GetCharacter<Character_TwoSisters>(1);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // verify visibility
        await room.StartGameAsync();
        AreEqual(typeof(Character_Unknown), sister1.GetSeenRole(room, wolf));
        AreEqual(typeof(Character_Unknown), sister1.GetSeenRole(room, vill));
        AreEqual(typeof(Character_Unknown), wolf.GetSeenRole(room, sister1));
        AreEqual(typeof(Character_Unknown), vill.GetSeenRole(room, sister1));
        AreEqual(typeof(Character_TwoSisters), sister1.GetSeenRole(room, sister2));
    }
}
