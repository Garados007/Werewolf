using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Tools;
using Werewolf.Theme.Default;

namespace Werewolf.Default.Test.Roles
{
    using Roles = Werewolf.Theme.Default.Roles;

    [TestClass]
    public class TwoSistersTest
    {
        [TestMethod]
        public async Task TwoSistersDiscussion()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.TwoSisters>(2)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var sister1 = room.GetUserWithRole<Roles.TwoSisters>(0);
            var sister2 = room.GetUserWithRole<Roles.TwoSisters>(1);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.TwoSisterDiscussionPhase>().ConfigureAwait(false);

            // vote for continue
            {
                sister1.Role!.ExpectVisibility<Roles.TwoSisters>(sister2.Role!);
                var voting = room.ExpectVoting<Theme.Default.Phases.TwoSisterDiscussionPhase.DiscussionEnd>();
                voting.Vote(room, sister1.User.Id, 0);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task TwoSistersLostAbilityAfterOldManDiesFromVillage()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.TwoSisters>(2)
                .InitRoles<Roles.OldMan>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var sister1 = room.GetUserWithRole<Roles.TwoSisters>(0);
            var sister2 = room.GetUserWithRole<Roles.TwoSisters>(1);
            var oldman = room.GetUserWithRole<Roles.OldMan>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired oneselect spy
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.TwoSisterDiscussionPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // old man dies
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, oldman);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            }

            // two sister discussion phase is now lost
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CheckVisibility()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.TwoSisters>(2)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill = room.GetUserWithRole<Roles.Villager>(0);
            var sister1 = room.GetUserWithRole<Roles.TwoSisters>(0);
            var sister2 = room.GetUserWithRole<Roles.TwoSisters>(1);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // verify visibility
            await room.StartGameAsync().ConfigureAwait(false);
            sister1.Role!.ExpectVisibility<Roles.Unknown>(wolf.Role!);
            sister1.Role!.ExpectVisibility<Roles.Unknown>(vill.Role!);
            wolf.Role!.ExpectVisibility<Roles.Unknown>(sister1.Role!);
            vill.Role!.ExpectVisibility<Roles.Unknown>(sister1.Role!);
            sister1.Role!.ExpectVisibility<Roles.Unknown>(sister2.Role!);
        }
    }
}