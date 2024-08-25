using Test.Tools;
using Theme.werewolf;

namespace Test.Logic.Modes.Werewolf;

[TestClass]
public class OracleTest
{
    [TestMethod]
    public async Task OracleViewWolfAndVillager()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Oracle>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var oracle = room.GetCharacter<Character_Oracle>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_OracleView>(room.Phase?.CurrentScene);

        // oracle vote and select wolf
        {
            AreEqual(typeof(Character_Unknown), wolf.GetSeenRole(room, oracle));
            var voting = room.ExpectVoting<Voting_OracleView>();
            IsNull(voting.Vote(room, oracle, wolf));
            voting.FinishVoting(room);
            AreEqual(typeof(Character_Werewolf), wolf.GetSeenRole(room, oracle));
            AreEqual(typeof(Character_Unknown), vill1.GetSeenRole(room, oracle));
            AreEqual(typeof(Character_Unknown), vill2.GetSeenRole(room, oracle));
        }

        room.Continue();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_OracleView>(room.Phase?.CurrentScene);

        // oracle vote and select villager
        {
            AreEqual(typeof(Character_Unknown), vill1.GetSeenRole(room, oracle));
            var voting = room.ExpectVoting<Voting_OracleView>();
            IsNull(voting.Vote(room, oracle, vill1));
            voting.FinishVoting(room);
            AreEqual(typeof(Character_Werewolf), wolf.GetSeenRole(room, oracle));
            AreEqual(typeof(Character_Villager), vill1.GetSeenRole(room, oracle));
            AreEqual(typeof(Character_Unknown), vill2.GetSeenRole(room, oracle));
        }

        room.Continue();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_OracleView>(room.Phase?.CurrentScene);

        // oracle vote and select villager
        {
            AreEqual(typeof(Character_Unknown), vill2.GetSeenRole(room, oracle));
            var voting = room.ExpectVoting<Voting_OracleView>();
            IsNull(voting.Vote(room, oracle, vill2));
            voting.FinishVoting(room);
            AreEqual(typeof(Character_Werewolf), wolf.GetSeenRole(room, oracle));
            AreEqual(typeof(Character_Villager), vill1.GetSeenRole(room, oracle));
            AreEqual(typeof(Character_Villager), vill2.GetSeenRole(room, oracle));
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }
    }

    [TestMethod]
    public async Task OracleLostAbilityAfterOldManDiesFromVillage()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Oracle>(1)
            .InitChars<Character_OldMan>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var oracle = room.GetCharacter<Character_Oracle>(0);
        var oldman = room.GetCharacter<Character_OldMan>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired oneselect spy
        await room.StartGameAsync();
        IsInstanceOfType<Scene_OracleView>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // old man dies
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, oldman));
            voting.FinishVoting(room);
            // he have no more oracle phase
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }
    }

    [TestMethod]
    public async Task CheckVisibility()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Oracle>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var oracle = room.GetCharacter<Character_Oracle>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // verify visibility
        await room.StartGameAsync();
        AreEqual(typeof(Character_Unknown), oracle.GetSeenRole(room, wolf));
        AreEqual(typeof(Character_Unknown), oracle.GetSeenRole(room, vill));
        AreEqual(typeof(Character_Unknown), wolf.GetSeenRole(room, oracle));
        AreEqual(typeof(Character_Unknown), vill.GetSeenRole(room, oracle));
    }
}
