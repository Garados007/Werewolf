using Test.Tools;
using Theme.werewolf;

namespace Test.Logic.Modes.Werewolf;

[TestClass]
public class HealerTest
{
    [TestMethod]
    public async Task HealerDoesnotProtect()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Healer>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var healer = room.GetCharacter<Character_Healer>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Healer>(room.Phase?.CurrentScene);

        // healer protects the wrong one
        {
            var voting = room.ExpectVoting<Voting_HealerProtection>();
            IsNull(voting.Vote(room, healer, vill2));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }

        // wolf kills
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(vill1.Enabled);
        }
    }

    [TestMethod]
    public async Task HealerProtects()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Healer>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var healer = room.GetCharacter<Character_Healer>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Healer>(room.Phase?.CurrentScene);

        // healer protects the right one
        {
            var voting = room.ExpectVoting<Voting_HealerProtection>();
            IsNull(voting.Vote(room, healer, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }

        // wolf kills
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsTrue(vill1.Enabled);
        }
    }

    [TestMethod]
    public async Task HealerCannotVoteTheSameOneTwice()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Healer>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var healer = room.GetCharacter<Character_Healer>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Healer>(room.Phase?.CurrentScene);

        // healer protects one
        {
            var voting = room.ExpectVoting<Voting_HealerProtection>();
            IsNull(voting.Vote(room, healer, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            room.Continue(true);
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            room.Continue(true);
            IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
            room.Continue(true);
            IsInstanceOfType<Scene_Healer>(room.Phase?.CurrentScene);
        }

        // healer cannot protect the same one again
        {
            var voting = room.ExpectVoting<Voting_HealerProtection>();
            voting.CannotVote(room, healer, vill1);
            Assert.IsNull(voting.Vote(room, healer, vill2));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
            room.Continue(true);
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            room.Continue(true);
            IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
            room.Continue(true);
            IsInstanceOfType<Scene_Healer>(room.Phase?.CurrentScene);
        }

        // healer can now protect the first one again
        {
            var voting = room.ExpectVoting<Voting_HealerProtection>();
            Assert.IsNull(voting.Vote(room, healer, vill1));
            voting.FinishVoting(room);
        }
    }

    [TestMethod]
    public async Task HealerCannotProtectFromHunter()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Healer>(1)
            .InitChars<Character_Hunter>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var healer = room.GetCharacter<Character_Healer>(0);
        var hunter = room.GetCharacter<Character_Hunter>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Healer>(room.Phase?.CurrentScene);

        // healer protects one
        {
            var voting = room.ExpectVoting<Voting_HealerProtection>();
            IsNull(voting.Vote(room, healer, vill1));
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

        // hunter kills protected one
        {
            var voting = room.ExpectVoting<Voting_HunterKill>();
            IsNull(voting.Vote(room, hunter, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(vill1.Enabled);
        }
    }

    [TestMethod]
    public async Task HealerCannotProtectFromWitch()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Healer>(1)
            .InitChars<Character_Witch>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var healer = room.GetCharacter<Character_Healer>(0);
        var witch = room.GetCharacter<Character_Witch>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Healer>(room.Phase?.CurrentScene);

        // healer protects one
        {
            var voting = room.ExpectVoting<Voting_HealerProtection>();
            IsNull(voting.Vote(room, healer, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }

        // wolf kills another one
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill2));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Witch>(room.Phase?.CurrentScene);
        }

        // witch kills protected one
        {
            var voting = room.ExpectVoting<Voting_Witch_DeathPotion>();
            IsNull(voting.Vote(room, witch, vill1));
            voting.FinishVoting(room);
            room.Continue(true);
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            IsFalse(vill1.Enabled);
            IsFalse(vill2.Enabled);
        }
    }


    [TestMethod]
    public async Task HealerLostAbilityAfterOldManDiesFromVillage()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Healer>(1)
            .InitChars<Character_OldMan>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var healer = room.GetCharacter<Character_Healer>(0);
        var oldman = room.GetCharacter<Character_OldMan>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Healer>(room.Phase?.CurrentScene);
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
            // because there are no more healer phases we continue to the werewolf phase
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
            .InitChars<Character_Healer>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var healer = room.GetCharacter<Character_Healer>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // verify visibility
        await room.StartGameAsync();
        AreSame(typeof(Character_Unknown), healer.GetSeenRole(room, wolf));
        AreSame(typeof(Character_Unknown), healer.GetSeenRole(room, vill));
        AreSame(typeof(Character_Unknown), wolf.GetSeenRole(room, healer));
        AreSame(typeof(Character_Unknown), vill.GetSeenRole(room, healer));
    }
}
