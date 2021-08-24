using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Tools;
using Werewolf.Theme.Default;

namespace Werewolf.Default.Test.Roles
{
    using Roles = Werewolf.Theme.Default.Roles;

    [TestClass]
    public class AmorTest
    {
        [TestMethod]
        public async Task AmorVotesTwoVillageAndOneIsKilledAtNight()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Amor>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var amor = room.GetUserWithRole<Roles.Amor>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.AmorPhase>();

            // amor vote 
            {
                var voting1 = room.ExpectVoting<Theme.Default.Phases.AmorPhase.AmorPick>(0);
                var voting2 = room.ExpectVoting<Theme.Default.Phases.AmorPhase.AmorPick>(1);
                voting1.Vote(room, amor, vill1);
                voting2.Vote(room, amor, vill2);
                await voting1.FinishVotingAsync(room).ConfigureAwait(false);
                await voting2.FinishVotingAsync(room).ConfigureAwait(false);
                //validate tag
                vill1.Role!.ExpectTag(room, "loved");
                vill2.Role!.ExpectTag(room, "loved");
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);

            // werewolf vote
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.NextPhaseAsync().ConfigureAwait(false);
                vill1.Role!.ExpectLiveState(Theme.KillState.Killed);
                vill2.Role!.ExpectLiveState(Theme.KillState.Killed);
            }
        }

        [TestMethod]
        public async Task SpecialWinCondition()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.Amor>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill = room.GetUserWithRole<Roles.Villager>(0);
            var amor = room.GetUserWithRole<Roles.Amor>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.AmorPhase>();

            // amor vote 
            {
                var voting1 = room.ExpectVoting<Theme.Default.Phases.AmorPhase.AmorPick>(0);
                var voting2 = room.ExpectVoting<Theme.Default.Phases.AmorPhase.AmorPick>(1);
                voting1.Vote(room, amor, amor);
                voting2.Vote(room, amor, wolf);
                await voting1.FinishVotingAsync(room).ConfigureAwait(false);
                await voting2.FinishVotingAsync(room).ConfigureAwait(false);
                //validate tag
                amor.Role!.ExpectTag(room, "loved");
                wolf.Role!.ExpectTag(room, "loved");
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);

            // werewolf vote
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.NextPhaseAsync().ConfigureAwait(false);
                room.ExpectWinner(amor, wolf);
            }
        }

        [TestMethod]
        public async Task CheckVisibility()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.Amor>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill = room.GetUserWithRole<Roles.Villager>(0);
            var amor = room.GetUserWithRole<Roles.Amor>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // verify visibility
            await room.StartGameAsync().ConfigureAwait(false);
            amor.Role!.ExpectVisibility<Roles.Unknown>(wolf.Role!);
            amor.Role!.ExpectVisibility<Roles.Unknown>(vill.Role!);
            wolf.Role!.ExpectVisibility<Roles.Unknown>(amor.Role!);
            vill.Role!.ExpectVisibility<Roles.Unknown>(amor.Role!);
        }
    }
}