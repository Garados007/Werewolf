using System.Reflection;
using Test.Tools;
using Theme.werewolf;
using Werewolf.Theme.Labels;

namespace Test.Logic.Modes.Werewolf;

[TestClass]
public class GirlTest
{
    private static void SetSeed(int? value)
    {
        var type = typeof(Voting_GirlSpy);
        var field = type.GetField("Seed", BindingFlags.Static | BindingFlags.NonPublic);
        if (field is null)
            Assert.Inconclusive("Debug build of tested assembly required");
        else field.SetValue(null, value);
    }

    [TestMethod]
    public async Task GirlSpyNothingHappens()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Girl>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var girl = room.GetCharacter<Character_Girl>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        SetSeed(1000);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // girl vote and select spy
        {
            var voting = room.ExpectVoting<Voting_GirlSpy>();
            IsNull(voting.Vote<Option_Spy>(room, girl));
            voting.FinishVoting(room);
            AreSame(typeof(Character_Unknown), wolf.GetSeenRole(room, girl));
            AreSame(typeof(Character_Unknown), girl.GetSeenRole(room, wolf));
        }
    }

    [TestMethod]
    public async Task GirlSpyAndGotCatched()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Girl>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var girl = room.GetCharacter<Character_Girl>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        SetSeed(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // girl vote and select spy
        {
            var voting = room.ExpectVoting<Voting_GirlSpy>();
            IsNull(voting.Vote<Option_Spy>(room, girl));
            voting.FinishVoting(room);
            AreSame(typeof(Character_Unknown), wolf.GetSeenRole(room, girl));
            AreSame(typeof(Character_Girl), girl.GetSeenRole(room, wolf));
        }
    }

    [TestMethod]
    public async Task GirlSpyAndSeeWolf()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Girl>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var girl = room.GetCharacter<Character_Girl>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        SetSeed(100);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // girl vote and select spy
        {
            var voting = room.ExpectVoting<Voting_GirlSpy>();
            IsNull(voting.Vote<Option_Spy>(room, girl));
            voting.FinishVoting(room);
            AreSame(typeof(Character_Werewolf), wolf.GetSeenRole(room, girl));
            AreSame(typeof(Character_Unknown), girl.GetSeenRole(room, wolf));
        }
    }

    [TestMethod]
    public async Task GirlSpyAndGotDetectedAndCanSeeWolf()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Girl>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var girl = room.GetCharacter<Character_Girl>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        SetSeed(10);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // girl vote and select spy
        {
            var voting = room.ExpectVoting<Voting_GirlSpy>();
            IsNull(voting.Vote<Option_Spy>(room, girl));
            voting.FinishVoting(room);
            AreSame(typeof(Character_Werewolf), wolf.GetSeenRole(room, girl));
            AreSame(typeof(Character_Girl), girl.GetSeenRole(room, wolf));
        }
    }

    [TestMethod]
    public async Task GirlDoNothing()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Girl>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var girl = room.GetCharacter<Character_Girl>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired oneselect spy
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // girl vote and select do nothing
        {
            var voting = room.ExpectVoting<Voting_GirlSpy>();
            IsNull(voting.Vote<Option_None>(room, girl));
            voting.FinishVoting(room);
            AreSame(typeof(Character_Unknown), wolf.GetSeenRole(room, girl));
            AreSame(typeof(Character_Unknown), girl.GetSeenRole(room, wolf));
        }
    }

    [TestMethod]
    public async Task GirlLostAbilityAfterOldManDiesFromVillage()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Girl>(1)
            .InitChars<Character_OldMan>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var girl = room.GetCharacter<Character_Girl>(0);
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
            oldman.ExpectLabel<ICharacterLabel, Label_Killed>();
        }

        // girl has no voting abilities
        room.ExpectNoVoting<Voting_GirlSpy>();
    }

    [TestMethod]
    public async Task CheckVisibility()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Girl>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var girl = room.GetCharacter<Character_Girl>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // verify visibility
        await room.StartGameAsync();
        AreSame(typeof(Character_Unknown), girl.GetSeenRole(room, wolf));
        AreSame(typeof(Character_Unknown), girl.GetSeenRole(room, vill));
        AreSame(typeof(Character_Unknown), wolf.GetSeenRole(room, girl));
        AreSame(typeof(Character_Unknown), vill.GetSeenRole(room, girl));
    }
}
