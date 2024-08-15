using Test.Tools;
using Theme.werewolf;

namespace Test.Logic.Modes.Werewolf;

[TestClass]
public class WitchTest
{
    [TestMethod]
    public async Task WitchProtectsVictim()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Witch>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var witch = room.GetCharacter<Character_Witch>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf voting and kill villager
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);
        }

        // witch protects victim
        {
            var voting = room.ExpectVoting<Voting_Witch_HealPotion>();
            IsNull(voting.Vote(room, witch, vill1));
            voting.FinishVoting(room);
            room.Continue(true);
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsTrue(vill1.Enabled);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf voting and kill villager
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);
        }

        // witch has no longer the ability to protect
        room.ExpectNoVoting<Voting_Witch_HealPotion>();
    }

    [TestMethod]
    public async Task WitchDoesntProtectVictim()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Witch>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var witch = room.GetCharacter<Character_Witch>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf voting and kill villager
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);
        }

        // witch doesn't protect victim
        {
            _ = room.ExpectVoting<Voting_Witch_HealPotion>();
            room.Continue(true);
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(vill1.Enabled);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf voting and kill villager
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill2));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);
        }

        // witch has the ability to protect now
        _ = room.ExpectVoting<Voting_Witch_HealPotion>();
    }

    [TestMethod]
    public async Task WitchKillsAnotherOne()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(3)
            .InitChars<Character_Witch>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var vill3 = room.GetCharacter<Character_Villager>(2);
        var witch = room.GetCharacter<Character_Witch>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf voting and kill villager
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);
        }

        // witch kills other player
        {
            var voting = room.ExpectVoting<Voting_Witch_DeathPotion>();
            IsNull(voting.Vote(room, witch, vill2));
            voting.FinishVoting(room);
            room.Continue(true);
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(vill1.Enabled);
            IsFalse(vill2.Enabled);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf voting and kill villager
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill3));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);
        }

        // witch has no longer the ability to kill another one
        room.ExpectNoVoting<Voting_Witch_DeathPotion>();
    }


    [TestMethod]
    public async Task WitchDoesntKillAnotherOne()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(3)
            .InitChars<Character_Witch>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var vill3 = room.GetCharacter<Character_Villager>(2);
        var witch = room.GetCharacter<Character_Witch>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf voting and kill villager
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);
        }

        // witch doesn't kill other player
        {
            var voting = room.ExpectVoting<Voting_Witch_DeathPotion>();
            room.Continue(true);
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(vill1.Enabled);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf voting and kill villager
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill3));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);
        }

        // witch can kill another one again
        room.ExpectVoting<Voting_Witch_DeathPotion>();
    }

    [TestMethod]
    public async Task WitchKillsAndWin()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Witch>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var witch = room.GetCharacter<Character_Witch>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf voting and kill villager
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);
        }

        // witch kills wolf
        {
            var voting = room.ExpectVoting<Voting_Witch_DeathPotion>();
            IsNull(voting.Vote(room, witch, wolf));
            voting.FinishVoting(room);
            room.Continue(true);
            room.ExpectWinner(witch, vill1);
        }
    }

    [TestMethod]
    public async Task WitchKillsAndLoose()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Witch>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var witch = room.GetCharacter<Character_Witch>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf voting and kill witch
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, witch));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);
        }

        // witch kills wolf
        {
            var voting = room.ExpectVoting<Voting_Witch_DeathPotion>();
            IsNull(voting.Vote(room, witch, vill1));
            voting.FinishVoting(room);
            room.Continue(true);
            room.ExpectWinner(wolf);
        }
    }


    [TestMethod]
    public async Task WitchLostAbilityAfterOldManDiesFromVillage()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Witch>(1)
            .InitChars<Character_OldMan>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var witch = room.GetCharacter<Character_Witch>(0);
        var oldman = room.GetCharacter<Character_OldMan>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired oneselect spy
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);
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

        // witch has no special phase anymore
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
    }

    [TestMethod]
    public async Task CheckVisibility()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Witch>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var witch = room.GetCharacter<Character_Witch>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // verify visibility
        await room.StartGameAsync();
        AreSame(typeof(Character_Unknown), witch.GetSeenRole(room, wolf));
        AreSame(typeof(Character_Unknown), witch.GetSeenRole(room, vill));
        AreSame(typeof(Character_Unknown), wolf.GetSeenRole(room, witch));
        AreSame(typeof(Character_Unknown), vill.GetSeenRole(room, witch));
    }
}
