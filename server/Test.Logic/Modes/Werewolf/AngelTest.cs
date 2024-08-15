using Test.Tools;
using Theme.werewolf;

namespace Test.Logic.Modes.Werewolf;

[TestClass]
public class AngelTest
{
    [TestMethod]
    public async Task AngelWinAtDay()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Angel>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var angel = room.GetCharacter<Character_Angel>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // kill angel
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, angel));
            voting.FinishVoting(room);
            room.Continue();
            IsFalse(angel.Enabled);
            room.ExpectWinner(angel);
        }
    }

    [TestMethod]
    public async Task AngelWinAtNight()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Angel>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var angel = room.GetCharacter<Character_Angel>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // kill angel
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, angel));
            voting.FinishVoting(room);
            room.Continue();
            IsFalse(angel.Enabled);
            room.ExpectWinner(angel);
        }
    }

    [TestMethod]
    public async Task AngelWinWithVillage()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Angel>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var angel = room.GetCharacter<Character_Angel>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // kill angel
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, angel));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // kill wolf
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, wolf));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectWinner(vill1, vill2, angel);
        }
    }

    [TestMethod]
    public async Task AngelLooseWithVillage()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Angel>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var angel = room.GetCharacter<Character_Angel>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // kill angel
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, vill1, angel));
            voting.FinishVoting(room);
            room.Continue();
            IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);
        }

        room.Continue(true);
        IsInstanceOfType<Scene_Major>(room.Phase?.CurrentScene);
        room.Continue(true);
        IsInstanceOfType<Scene_DailyVote>(room.Phase?.CurrentScene);

        // kill villager
        {
            var voting = room.ExpectVoting<Voting_DailyVoting>();
            IsNull(voting.Vote(room, wolf, vill1));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectWinner(wolf);
        }
    }

    [TestMethod]
    public async Task CheckVisibility()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Angel>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var angel = room.GetCharacter<Character_Angel>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // verify visibility
        await room.StartGameAsync();
        AreSame(typeof(Character_Unknown), angel.GetSeenRole(room, wolf));
        AreSame(typeof(Character_Unknown), angel.GetSeenRole(room, vill));
        AreSame(typeof(Character_Unknown), wolf.GetSeenRole(room, angel));
        AreSame(typeof(Character_Unknown), vill.GetSeenRole(room, angel));
    }
}
