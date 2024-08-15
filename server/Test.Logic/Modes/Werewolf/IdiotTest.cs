using Test.Tools;
using Theme.werewolf;

namespace Test.Logic.Modes.Werewolf;

[TestClass]
public class IdiotTest
{
    [TestMethod]
    public async Task VillageCannotKillIdiot()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Idiot>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var idiot = room.GetCharacter<Character_Idiot>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village tries to kill the first time
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsTrue(voting.CanVote(room, idiot));
            IsNull(voting.Vote(room, vill1, idiot));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            IsTrue(idiot.Enabled);
            AreSame(typeof(Character_Idiot), idiot.GetSeenRole(room, vill1));
            AreSame(typeof(Character_Idiot), idiot.GetSeenRole(room, vill2));
            AreSame(typeof(Character_Idiot), idiot.GetSeenRole(room, wolf));
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village cannot mark idiot as target, idiot cannot vote any longer
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            voting.CannotVote(room, vill1, idiot);
            voting.CannotVote(room, wolf, idiot);
            IsFalse(voting.CanVote(room, idiot));
        }
    }

    [TestMethod]
    public async Task WolfesCanKillIdiot()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Idiot>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var idiot = room.GetCharacter<Character_Idiot>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf kills idiot
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, idiot));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(idiot.Enabled);
        }
    }

    [TestMethod]
    public async Task WolfesCanKillVisibleIdiot()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Idiot>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var idiot = room.GetCharacter<Character_Idiot>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village tries to kill idiot
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, idiot));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }

        // wolf kills idiot
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, idiot));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(idiot.Enabled);
        }
    }

    [TestMethod]
    public async Task WitchCanKillIdiot()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Idiot>(1)
            .InitChars<Character_Witch>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var idiot = room.GetCharacter<Character_Idiot>(0);
        var witch = room.GetCharacter<Character_Witch>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);

        // witch kills idiot
        {
            var voting = room.ExpectVoting<Voting_Witch_DeathPotion>();
            IsNull(voting.Vote(room, witch, idiot));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(idiot.Enabled);
        }
    }

    [TestMethod]
    public async Task WitchCanKillVisibleIdiot()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Idiot>(1)
            .InitChars<Character_Witch>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var idiot = room.GetCharacter<Character_Idiot>(0);
        var witch = room.GetCharacter<Character_Witch>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village tries to kill idiot
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, idiot));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            room.Continue(true);
            IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);
        }

        // witch kills idiot
        {
            var voting = room.ExpectVoting<Voting_Witch_DeathPotion>();
            IsNull(voting.Vote(room, witch, idiot));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(idiot.Enabled);
        }
    }

    [TestMethod]
    public async Task HunterCanKillIdiot()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Idiot>(1)
            .InitChars<Character_Hunter>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var idiot = room.GetCharacter<Character_Idiot>(0);
        var hunter = room.GetCharacter<Character_Hunter>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf kills hunter
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, hunter));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectSequence<Sequence_HunterKill>();
        }

        // hunter kills idiot
        {
            var voting = room.ExpectVoting<Voting_HunterKill>();
            IsNull(voting.Vote(room, hunter, idiot));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(idiot.Enabled);
        }
    }

    [TestMethod]
    public async Task HunterCanKillVisibleIdiot()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Idiot>(1)
            .InitChars<Character_Hunter>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var idiot = room.GetCharacter<Character_Idiot>(0);
        var hunter = room.GetCharacter<Character_Hunter>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village tries to kill idiot
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, idiot));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }

        // wolf kills hunter
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, hunter));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectSequence<Sequence_HunterKill>();
        }

        // hunter kills idiot
        {
            var voting = room.ExpectVoting<Voting_HunterKill>();
            IsNull(voting.Vote(room, hunter, idiot));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(idiot.Enabled);
        }
    }

    [TestMethod]
    public async Task RevealedIdiotCanDiscardMajor()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Idiot>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var idiot = room.GetCharacter<Character_Idiot>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);

        // the idiot becomes the new major
        {
            var voting = room.ExpectVoting<Voting_MajorSelection>();
            IsNull(voting.Vote(room, vill1, idiot));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
        }

        // village tries to kill idiot
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, idiot));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }

        // they should be no new major selection phase
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // and no major pick will happen again
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, vill2));
            IsNull(voting.Vote(room, vill2, vill1));
            voting.FinishVoting(room);
            _ = room.ExpectVoting<Voting_DailyVoting>();
            room.ExpectNoVoting<Voting_DailyVotingByMajor>();
        }

        // and the major role is never transmitted to the next player
        {
            room.Continue(true);
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, idiot));
            voting.FinishVoting(room);
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
            .InitChars<Character_Idiot>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var idiot = room.GetCharacter<Character_Idiot>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // verify visibility
        await room.StartGameAsync();
        AreSame(typeof(Character_Unknown), idiot.GetSeenRole(room, wolf));
        AreSame(typeof(Character_Unknown), idiot.GetSeenRole(room, vill));
        AreSame(typeof(Character_Unknown), wolf.GetSeenRole(room, idiot));
        AreSame(typeof(Character_Unknown), vill.GetSeenRole(room, idiot));
    }
}
