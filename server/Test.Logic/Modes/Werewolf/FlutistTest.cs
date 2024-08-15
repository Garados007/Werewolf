using Test.Tools;
using Theme.werewolf;
using Werewolf.Theme.Labels;

namespace Test.Logic.Modes.Werewolf;

[TestClass]
public class FlutistTest
{
    [TestMethod]
    public async Task FlutistCannotWinWithVillager()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Flutist>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var flutist = room.GetCharacter<Character_Flutist>(0);
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
            IsInstanceOfType<Scene_Flutist>(room.Phase?.CurrentScene);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village kills wolf
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill2, wolf));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Flutist>(room.Phase?.CurrentScene);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village kills flutist
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill2, flutist));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectWinner(vill1, vill2);
        }
    }

    [TestMethod]
    public async Task FlutistCannotWinWithWolf()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Flutist>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var flutist = room.GetCharacter<Character_Flutist>(0);
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
            IsInstanceOfType<Scene_Flutist>(room.Phase?.CurrentScene);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village kills villager
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill2, vill2));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Flutist>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village kills flutist
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, wolf, flutist));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectWinner(wolf);
        }
    }

    [TestMethod]
    public async Task FlutistCannotWinsAlone()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Flutist>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var flutist = room.GetCharacter<Character_Flutist>(0);
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
            IsInstanceOfType<Scene_Flutist>(room.Phase?.CurrentScene);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village kills wolf
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill2, wolf));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Flutist>(room.Phase?.CurrentScene);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village kills villager
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, flutist, vill2));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectWinner(flutist);
        }
    }

    [TestMethod]
    public async Task FlutistWinWithEnchantedTogether()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Flutist>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var flutist = room.GetCharacter<Character_Flutist>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Flutist>(room.Phase?.CurrentScene);

        // flutist votes villager
        {
            var voting = room.ExpectVoting<Voting_FlutistSelection>();
            IsNull(voting.Vote(room, flutist, vill1));
            voting.FinishVoting(room);

            room.ExpectNoVoting<Voting_FlutistSelection>();

            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            vill1.ExpectLabel<ICharacterLabel, Label_EnchantedByFlutist>();
        }

        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village kills villager
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, flutist, wolf));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectWinner(flutist);
        }
    }

    [TestMethod]
    public async Task FlutistHasTwoVotesInLargeGroups()
    {
        Inconclusive("This feature is currently disabled");
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(10)
            .InitChars<Character_Flutist>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var flutist = room.GetCharacter<Character_Flutist>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Flutist>(room.Phase?.CurrentScene);

        // flutist votes villager and wolf
        {
            var voting = room.ExpectVoting<Voting_FlutistSelection>();
            IsNull(voting.Vote(room, flutist, vill1));
            IsNull(voting.Vote(room, flutist, vill2));
            IsNotNull(voting.Vote(room, flutist, wolf));
            voting.FinishVoting(room);

            room.ExpectNoVoting<Voting_FlutistSelection>();

            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
            vill1.ExpectLabel<ICharacterLabel, Label_EnchantedByFlutist>();
            vill2.ExpectLabel<ICharacterLabel, Label_EnchantedByFlutist>();
        }
    }


    [TestMethod]
    public async Task DeadFlutistCanWinIfOnlyEnchanted()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Flutist>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var flutist = room.GetCharacter<Character_Flutist>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Flutist>(room.Phase?.CurrentScene);

        // flutist votes wolf
        {
            var voting = room.ExpectVoting<Voting_FlutistSelection>();
            IsNull(voting.Vote(room, flutist, wolf));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // village kills flutist
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, flutist));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }

        // wolf kills villager
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill1));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectWinner(flutist);
        }
    }

    [TestMethod]
    public async Task CheckVisibility()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Flutist>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var flutist = room.GetCharacter<Character_Flutist>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // verify visibility
        await room.StartGameAsync();
        AreSame(typeof(Character_Unknown), flutist.GetSeenRole(room, wolf));
        AreSame(typeof(Character_Unknown), flutist.GetSeenRole(room, vill));
        AreSame(typeof(Character_Unknown), wolf.GetSeenRole(room, flutist));
        AreSame(typeof(Character_Unknown), vill.GetSeenRole(room, flutist));
    }
}
