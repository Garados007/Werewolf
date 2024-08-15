using Test.Tools;
using Theme.werewolf;

namespace Test.Logic.Modes.Werewolf;

[TestClass]
public class ScapeGoatTest
{
    [TestMethod]
    public async Task ScapeGoatKilledAtDayAndSelectsSome()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(3)
            .InitChars<Character_ScapeGoat>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var vill3 = room.GetCharacter<Character_Villager>(2);
        var goat = room.GetCharacter<Character_ScapeGoat>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // create a tie and therefore kill the scapegoat
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, vill2));
            IsNull(voting.Vote(room, vill2, vill1));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectSequence<Sequence_ScapeGoatKill>();
        }

        // scape goat selects some
        {
            var voting = room.ExpectVoting<Voting_ScapeGoat>();
            IsNull(voting.Vote(room, goat, vill1));
            IsNull(voting.Vote(room, goat, vill3));
            IsNull(voting.Vote(room, goat, -1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            IsFalse(goat.Enabled);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // only selected one is able to vote this time
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsTrue(voting.CanVote(room, vill1));
            IsFalse(voting.CanVote(room, vill2));
            IsTrue(voting.CanVote(room, vill3));
            IsFalse(voting.CanVote(room, wolf));
            IsNull(voting.Vote(room, vill1, vill2));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            IsFalse(vill2.Enabled);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // everyone can vote again
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsTrue(voting.CanVote(room, vill1));
            IsTrue(voting.CanVote(room, vill3));
            IsTrue(voting.CanVote(room, wolf));
        }
    }

    [TestMethod]
    public async Task ScapeGoatKilledAtDayAndSelectsNoone()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_ScapeGoat>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var goat = room.GetCharacter<Character_ScapeGoat>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // create a tie and therefore kill the scapegoat
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, vill2));
            IsNull(voting.Vote(room, vill2, vill1));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectSequence<Sequence_ScapeGoatKill>();
        }

        // scape goat selects noone
        {
            var voting = room.ExpectVoting<Voting_ScapeGoat>();
            IsNull(voting.Vote(room, goat, -1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            IsFalse(goat.Enabled);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // everyone can vote again
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsTrue(voting.CanVote(room, vill1));
            IsTrue(voting.CanVote(room, vill2));
            IsTrue(voting.CanVote(room, wolf));
        }
    }

    [TestMethod]
    public async Task ScapeGoatKilledAtDayAndSelectOneButGotKilled()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_ScapeGoat>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var goat = room.GetCharacter<Character_ScapeGoat>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // create a tie and therefore kill the scapegoat
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, vill2));
            IsNull(voting.Vote(room, vill2, vill1));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectSequence<Sequence_ScapeGoatKill>();
        }

        // scape goat selects some
        {
            var voting = room.ExpectVoting<Voting_ScapeGoat>();
            IsNull(voting.Vote(room, goat, vill1));
            IsNull(voting.Vote(room, goat, -1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            IsFalse(goat.Enabled);
        }

        // wolf kills voter
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(vill1.Enabled);
        }

        room.Continue(true);
        // no one is able to vote, because the only one got killed => Skip daily phase
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // everyone can vote again
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsTrue(voting.CanVote(room, vill2));
            IsTrue(voting.CanVote(room, wolf));
        }
    }

    [TestMethod]
    public async Task ScapeGoatLostAbilityAfterOldManDiesFromVillage()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_ScapeGoat>(1)
            .InitChars<Character_OldMan>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var goat = room.GetCharacter<Character_ScapeGoat>(0);
        var oldman = room.GetCharacter<Character_OldMan>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
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
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // create a tie, scape goat will no longer die
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, vill2));
            IsNull(voting.Vote(room, vill2, vill1));
            voting.FinishVoting(room);
            // scape goat was not killed and there is no special phase
            room.Continue();
            IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
            room.ExpectNoVoting<Voting_ScapeGoat>();
            IsFalse(goat.Enabled);
        }
    }

    [TestMethod]
    public async Task CheckVisibility()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_ScapeGoat>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var scapegoat = room.GetCharacter<Character_ScapeGoat>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // verify visibility
        await room.StartGameAsync();
        AreEqual(typeof(Character_Unknown), scapegoat.GetSeenRole(room, wolf));
        AreEqual(typeof(Character_Unknown), scapegoat.GetSeenRole(room, vill));
        AreEqual(typeof(Character_Unknown), wolf.GetSeenRole(room, scapegoat));
        AreEqual(typeof(Character_Unknown), vill.GetSeenRole(room, scapegoat));
    }
}
