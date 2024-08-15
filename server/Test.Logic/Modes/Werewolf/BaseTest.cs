using Test.Tools;
using Theme.werewolf;
using Werewolf.Theme;
using Werewolf.Theme.Labels;

namespace Test.Logic.Modes.Werewolf;

[TestClass]
public class BaseTest
{
    [TestMethod]
    public async Task VillagerWinFromVillageContest()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Werewolf>(1);
        AreEqual(3, runner.GameRoom.Users.Count);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // vote wolf and finish voting
        var voting = room.ExpectVoting<Voting_DailyVoting>();
        IsNull(voting.Vote(room, vill1, wolf));
        IsNull(voting.Vote(room, vill2, wolf));
        voting.FinishVoting(room);
        room.Continue();

        // now the villager should win the game
        runner.CollectEvent<global::Werewolf.Theme.Events.GameEnd>();
        room.ExpectWinner(vill1, vill2);
    }

    [TestMethod]
    public async Task WolfWinFromNight()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // vote villager and finish voting
        var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
        IsNull(voting.Vote(room, wolf, vill));
        voting.FinishVoting(room);
        room.Continue();

        // now the wolf should win the game
        runner.CollectEvent<global::Werewolf.Theme.Events.GameEnd>();
        room.ExpectWinner(wolf);
    }

    [TestMethod]
    public async Task MajorPick()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(4)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var vill3 = room.GetCharacter<Character_Villager>(2);
        var vill4 = room.GetCharacter<Character_Villager>(3);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);

        // elect vill1 as major
        {
            var voting = room.ExpectVoting<Voting_MajorSelection>();
            IsNull(voting.Vote(room, vill1, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
        }

        // create a special voting condition with no clear result
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, vill3));
            IsNull(voting.Vote(room, vill2, vill3));
            IsNull(voting.Vote(room, vill3, vill4));
            IsNull(voting.Vote(room, vill4, vill4));
            IsNull(voting.Vote(room, wolf, vill2));
            voting.FinishVoting(room);
        }

        // the major has to pick now one of the highest results (in this case vill3, vill4)
        {
            room.ExpectNoVoting<Voting_DailyVoting>();
            var voting = room.ExpectVoting<Voting_DailyVotingByMajor>();
            IsFalse(voting.Options.Any(x => x.option is CharacterOption opt && opt.Character == vill2));
            IsTrue(voting.Options.Any(x => x.option is CharacterOption opt && opt.Character == vill3));
            IsTrue(voting.Options.Any(x => x.option is CharacterOption opt && opt.Character == vill4));
            IsNull(voting.Vote(room, vill1, vill3));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            IsFalse(vill3.Enabled);
            IsTrue(vill4.Enabled);
        }
    }

    [TestMethod]
    public async Task MajorInheritDay()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(4)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var vill3 = room.GetCharacter<Character_Villager>(2);
        var vill4 = room.GetCharacter<Character_Villager>(3);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);

        // elect vill1 as major
        {
            var voting = room.ExpectVoting<Voting_MajorSelection>();
            IsNull(voting.Vote(room, vill1, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
        }

        // kill the major
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill2, vill1));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectSequence<Sequence_MajorInheritance>();
        }

        // the major can now be inherited
        {
            IsTrue(vill1.Enabled);
            vill1.ExpectLabel<ICharacterLabel, Label_Killed>();
            var voting = room.ExpectVoting<Voting_InheritMajor>();
            IsNull(voting.Vote(room, vill1, vill3));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            IsFalse(vill1.Enabled);
            vill3.ExpectLabel<ICharacterLabel, Label_Major>();
        }
    }

    [TestMethod]
    public async Task MajorInheritNight()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(4)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var vill3 = room.GetCharacter<Character_Villager>(2);
        var vill4 = room.GetCharacter<Character_Villager>(3);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);

        // elect vill1 as major
        {
            var voting = room.ExpectVoting<Voting_MajorSelection>();
            IsNull(voting.Vote(room, vill1, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // kill the major
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill1));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectSequence<Sequence_MajorInheritance>();
        }

        // the major can now be inherited
        {
            IsTrue(vill1.Enabled);
            vill1.ExpectLabel<ICharacterLabel, Label_Killed>();
            var voting = room.ExpectVoting<Voting_InheritMajor>();
            IsNull(voting.Vote(room, vill1, vill3));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
            IsFalse(vill1.Enabled);
            vill3.ExpectLabel<ICharacterLabel, Label_Major>();
        }
    }

    [TestMethod]
    public async Task CheckVisibility()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // verify visibility
        await room.StartGameAsync();
        AreSame(typeof(Character_Unknown), vill.GetSeenRole(room, wolf));
        AreSame(typeof(Character_Unknown), wolf.GetSeenRole(room, vill));
    }

    [TestMethod]
    public async Task ElectMajor()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);

        // vote villager and finish voting
        var voting = room.ExpectVoting<Voting_MajorSelection>();
        IsNull(voting.Vote(room, wolf, vill));
        voting.FinishVoting(room);
        vill.ExpectLabel<ICharacterLabel, Label_Major>();
    }
}
