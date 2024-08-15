using Test.Tools;
using Theme.werewolf;
using Werewolf.Theme.Labels;

namespace Test.Logic.Modes.Werewolf;

[TestClass]
public class AmorTest
{
    [TestMethod]
    public async Task AmorVotesTwoVillageAndOneIsKilledAtNight()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(2)
            .InitChars<Character_Amor>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill1 = room.GetCharacter<Character_Villager>(0);
        var vill2 = room.GetCharacter<Character_Villager>(1);
        var amor = room.GetCharacter<Character_Amor>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Amor>(room.Phase?.CurrentScene);

        // amor vote
        {
            var voting = room.ExpectVoting<Voting_AmorSelection>();
            IsNull(voting.Vote(room, amor, vill1));
            IsNull(voting.Vote(room, amor, vill2));
            IsNull(voting.Vote(room, amor, -1));
            voting.FinishVoting(room);
            //validate tag
            vill1.ExpectLabel<ICharacterLabel, Label_LovedOne>(0, x => x.Member_amor == amor);
            vill1.ExpectLabel<ICharacterLabel, Label_LovedCrush>(0, x => x.Member_amor == amor && x.Member_partner == vill2);
            vill2.ExpectLabel<ICharacterLabel, Label_LovedOne>(0, x => x.Member_amor == amor);
            vill2.ExpectLabel<ICharacterLabel, Label_LovedCrush>(0, x => x.Member_amor == amor && x.Member_partner == vill1);
        }

        room.Continue();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // werewolf vote
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill1));
            voting.FinishVoting(room);
            room.Continue();
            IsFalse(vill1.Enabled);
            IsFalse(vill2.Enabled);
        }
    }

    [TestMethod]
    public async Task SpecialWinCondition()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .DefaultConfig()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Amor>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var amor = room.GetCharacter<Character_Amor>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // skip phases until we have our desired one
        await room.StartGameAsync();
        IsInstanceOfType<Scene_Amor>(room.Phase?.CurrentScene);

        // amor vote
        {
            var voting = room.ExpectVoting<Voting_AmorSelection>();
            IsNull(voting.Vote(room, amor, amor));
            IsNull(voting.Vote(room, amor, wolf));
            IsNull(voting.Vote(room, amor, -1));
            voting.FinishVoting(room);
            //validate tag
            amor.ExpectLabel<ICharacterLabel, Label_LovedOne>(0, x => x.Member_amor == amor);
            amor.ExpectLabel<ICharacterLabel, Label_LovedCrush>(0, x => x.Member_amor == amor && x.Member_partner == wolf);
            wolf.ExpectLabel<ICharacterLabel, Label_LovedOne>(0, x => x.Member_amor == amor);
            wolf.ExpectLabel<ICharacterLabel, Label_LovedCrush>(0, x => x.Member_amor == amor && x.Member_partner == amor);
        }

        room.Continue();
        IsInstanceOfType<Scene_Werewolf>(room.Phase?.CurrentScene);

        // werewolf vote
        {
            var voting = room.ExpectVoting<Voting_Werewolf_SelectTarget>();
            IsNull(voting.Vote(room, wolf, vill));
            voting.FinishVoting(room);
            room.Continue();
            room.ExpectWinner(amor, wolf);
        }
    }

    [TestMethod]
    public async Task CheckVisibility()
    {
        // create runner and fill with data
        var runner = new Runner<Mode_BasicWerewolf>()
            .InitChars<Character_Villager>(1)
            .InitChars<Character_Amor>(1)
            .InitChars<Character_Werewolf>(1);
        var room = runner.GameRoom;
        var vill = room.GetCharacter<Character_Villager>(0);
        var amor = room.GetCharacter<Character_Amor>(0);
        var wolf = room.GetCharacter<Character_Werewolf>(0);

        // verify visibility
        await room.StartGameAsync();
        AreSame(typeof(Character_Unknown), amor.GetSeenRole(room, wolf));
        AreSame(typeof(Character_Unknown), amor.GetSeenRole(room, vill));
        AreSame(typeof(Character_Unknown), wolf.GetSeenRole(room, amor));
        AreSame(typeof(Character_Unknown), vill.GetSeenRole(room, amor));
    }
}
