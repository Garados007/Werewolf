using Test.Tools;
using Theme.werewolf;
using Werewolf.Theme.Labels;

namespace Test.Logic.Modes.Werewolf;

[TestClass]
public class OldManTest
{
    [TestMethod]
    public async Task OldManTakesTwoTurnToKillFromWolf()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_OldMan>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var oldman = room.GetCharacter<Character_OldMan>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // wolf voting and kill old man, he survives
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, oldman));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsTrue(oldman.Enabled);
            room.Continue(true);
            IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
            room.Continue(true);
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }

        // wolf try to kill again, this time he dies
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, oldman));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(oldman.Enabled);
        }
    }

    [TestMethod]
    public async Task HunterCanKillOldMan()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_OldMan>(1)
            .InitChars<Character_Hunter>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var oldman = room.GetCharacter<Character_OldMan>(0);
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

        // hunter kills old man
        {
            var voting = room.ExpectVoting<Voting_HunterKill>();
            IsNull(voting.Vote(room, hunter, oldman));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(oldman.Enabled);
        }
    }

    [TestMethod]
    public async Task WitchKillsOldMan()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_OldMan>(1)
            .InitChars<Character_Witch>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var oldman = room.GetCharacter<Character_OldMan>(0);
        var witch = room.GetCharacter<Character_Witch>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);

        // witch kills old man
        {
            var voting = room.ExpectVoting<Voting_Witch_DeathPotion>();
            IsNull(voting.Vote(room, witch, oldman));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(oldman.Enabled);
        }
    }

    [TestMethod]
    public async Task DeadOldManCannotProtectIdiotFromVillageKill()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_OldMan>(1)
            .InitChars<Character_Idiot>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var oldman = room.GetCharacter<Character_OldMan>(0);
        var idiot = room.GetCharacter<Character_Idiot>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village kills old man in first try
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, oldman));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            IsFalse(oldman.Enabled);
            oldman.ExpectLabel<ICharacterLabel, Label_OldManKilledByVillage>();
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village kills idiot, idiot is now dead because old man was killed before
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, idiot));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            IsFalse(idiot.Enabled);
        }
    }

    [TestMethod]
    public async Task DeadOldManCannotProtectRevealedIdiot()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_OldMan>(1)
            .InitChars<Character_Idiot>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var oldman = room.GetCharacter<Character_OldMan>(0);
        var idiot = room.GetCharacter<Character_Idiot>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village tries to kill idiot, doesn't work
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, idiot));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            IsTrue(idiot.Enabled);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village kills old man, his wisdom is lost, idiot is killed too
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, oldman));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            IsFalse(oldman.Enabled);
            IsFalse(idiot.Enabled);
            oldman.ExpectLabel<ICharacterLabel, Label_OldManKilledByVillage>();
        }
    }

    [TestMethod]
    public async Task CheckVisibility()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_OldMan>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var oldman = room.GetCharacter<Character_OldMan>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // verify visibility
        await room.StartGameAsync();
        AreSame(typeof(Character_Unknown), oldman.GetSeenRole(room, wolf));
        AreSame(typeof(Character_Unknown), oldman.GetSeenRole(room, vill));
        AreSame(typeof(Character_Unknown), wolf.GetSeenRole(room, oldman));
        AreSame(typeof(Character_Unknown), vill.GetSeenRole(room, oldman));
    }
}
