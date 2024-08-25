using Test.Tools;
using Theme.werewolf;

namespace Test.Logic.Modes.Werewolf;

[TestClass]
public class HunterTest
{
    [TestMethod]
    public async Task HunterKilledAtNightAndKillPlayer()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Hunter>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var hunter = room.GetCharacter<Character_Hunter>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf voting and kill hunter
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, hunter));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectSequence<Sequence_HunterKill>();
        }

        // hunter kill villager
        {
            var voting = room.ExpectVoting<Voting_HunterKill>();
            IsNull(voting.Vote(room, hunter, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        }
    }

    [TestMethod]
    public async Task HunterKilledAtDayAndKillPlayer()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Hunter>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var hunter = room.GetCharacter<Character_Hunter>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // daily voting and kill hunter
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, wolf, hunter));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectSequence<Sequence_HunterKill>();
        }

        // hunter kill villager
        {
            var voting = room.ExpectVoting<Voting_HunterKill>();
            IsNull(voting.Vote(room, hunter, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }
    }

    [TestMethod]
    public async Task HunterKilledAtNightAndKillWolfAndWin()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Hunter>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var hunter = room.GetCharacter<Character_Hunter>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf voting and kill hunter
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, hunter));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectSequence<Sequence_HunterKill>();
        }

        // hunter kill wolf
        {
            var voting = room.ExpectVoting<Voting_HunterKill>();
            IsNull(voting.Vote(room, hunter, wolf));
            voting.FinishVoting(room);
            room.Continue();
            IsFalse(wolf.Enabled);
            room.ExpectWinner(vill1, vill2, hunter);
        }
    }

    [TestMethod]
    public async Task HunterKilledAtDayAndKillWolfAndWin()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Hunter>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var hunter = room.GetCharacter<Character_Hunter>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // daily voting and kill hunter
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, wolf, hunter));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectSequence<Sequence_HunterKill>();
        }

        // hunter kill wolf
        {
            var voting = room.ExpectVoting<Voting_HunterKill>();
            IsNull(voting.Vote(room, hunter, wolf));
            voting.FinishVoting(room);
            room.Continue();
            IsFalse(wolf.Enabled);
            room.ExpectWinner(vill1, vill2, hunter);
        }
    }

    [TestMethod]
    public async Task HunterKilledAtNightAndKillVillagerAndLoose()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Hunter>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var hunter = room.GetCharacter<Character_Hunter>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf voting and kill hunter
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, hunter));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectSequence<Sequence_HunterKill>();
        }

        // hunter kill villager
        {
            var voting = room.ExpectVoting<Voting_HunterKill>();
            IsNull(voting.Vote(room, hunter, vill));
            voting.FinishVoting(room);
            room.Continue();
            IsFalse(vill.Enabled);
            room.ExpectWinner(wolf);
        }
    }

    [TestMethod]
    public async Task HunterKilledAtDayAndKillVillagerAndLoose()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Hunter>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var hunter = room.GetCharacter<Character_Hunter>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // daily voting and kill hunter
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, wolf, hunter));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectSequence<Sequence_HunterKill>();
        }

        // hunter kill villager
        {
            var voting = room.ExpectVoting<Voting_HunterKill>();
            IsNull(voting.Vote(room, hunter, vill));
            voting.FinishVoting(room);
            room.Continue();
            IsFalse(vill.Enabled);
            room.ExpectWinner(wolf);
        }
    }

    [TestMethod]
    public async Task HunterLostAbilityAfterOldManDiesFromVillage()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Hunter>(1)
            .InitChars<Character_OldMan>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var hunter = room.GetCharacter<Character_Hunter>(0);
        var oldman = room.GetCharacter<Character_OldMan>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired oneselect spy
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

        // wolf kills hunter
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, hunter));
            voting.FinishVoting(room);
            // hunter lost its ability, no hunter phase anymore
            room.Continue();
            room.Continue();
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        }
    }

    [TestMethod]
    public async Task CheckVisibility()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Hunter>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var hunter = room.GetCharacter<Character_Hunter>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // verify visibility
        await room.StartGameAsync();
        AreSame(typeof(Character_Unknown), hunter.GetSeenRole(room, wolf));
        AreSame(typeof(Character_Unknown), hunter.GetSeenRole(room, vill));
        AreSame(typeof(Character_Unknown), wolf.GetSeenRole(room, hunter));
        AreSame(typeof(Character_Unknown), vill.GetSeenRole(room, hunter));
    }
}
