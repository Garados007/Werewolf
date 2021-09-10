using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Tools;
using Werewolf.Theme.Default;

namespace Werewolf.Default.Test.Roles
{
    using Roles = Werewolf.Theme.Default.Roles;

    [TestClass]
    public class OracleTest
    {
        [TestMethod]
        public async Task OracleViewWolfAndVillager()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Oracle>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var oracle = room.GetUserWithRole<Roles.Oracle>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.OraclePhase>();

            // oracle vote and select wolf
            {
                wolf.Role!.ExpectVisibility<Roles.Unknown>(oracle.Role!);
                var voting = room.ExpectVoting<Theme.Default.Phases.OraclePhase.OraclePick>();
                voting.Vote(room, oracle, wolf);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                wolf.Role!.ExpectVisibility<Roles.Werwolf>(oracle.Role!);
                vill1.Role!.ExpectVisibility<Roles.Unknown>(oracle.Role!);
                vill2.Role!.ExpectVisibility<Roles.Unknown>(oracle.Role!);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.OraclePhase>().ConfigureAwait(false);

            // oracle vote and select villager
            {
                vill1.Role!.ExpectVisibility<Roles.Unknown>(oracle.Role!);
                var voting = room.ExpectVoting<Theme.Default.Phases.OraclePhase.OraclePick>();
                voting.Vote(room, oracle, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                wolf.Role!.ExpectVisibility<Roles.Werwolf>(oracle.Role!);
                vill1.Role!.ExpectVisibility<Roles.Villager>(oracle.Role!);
                vill2.Role!.ExpectVisibility<Roles.Unknown>(oracle.Role!);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.OraclePhase>().ConfigureAwait(false);

            // oracle vote and select villager
            {
                vill2.Role!.ExpectVisibility<Roles.Unknown>(oracle.Role!);
                var voting = room.ExpectVoting<Theme.Default.Phases.OraclePhase.OraclePick>();
                voting.Vote(room, oracle, vill2);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                wolf.Role!.ExpectVisibility<Roles.Werwolf>(oracle.Role!);
                vill1.Role!.ExpectVisibility<Roles.Villager>(oracle.Role!);
                vill2.Role!.ExpectVisibility<Roles.Villager>(oracle.Role!);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task OracleLostAbilityAfterOldManDiesFromVillage()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Oracle>(1)
                .InitRoles<Roles.OldMan>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var oracle = room.GetUserWithRole<Roles.Oracle>(0);
            var oldman = room.GetUserWithRole<Roles.OldMan>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired oneselect spy
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.OraclePhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // old man dies
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, oldman);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                // he have no more oracle phase
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task CheckVisibility()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.Oracle>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill = room.GetUserWithRole<Roles.Villager>(0);
            var oracle = room.GetUserWithRole<Roles.Oracle>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // verify visibility
            await room.StartGameAsync().ConfigureAwait(false);
            oracle.Role!.ExpectVisibility<Roles.Unknown>(wolf.Role!);
            oracle.Role!.ExpectVisibility<Roles.Unknown>(vill.Role!);
            wolf.Role!.ExpectVisibility<Roles.Unknown>(oracle.Role!);
            vill.Role!.ExpectVisibility<Roles.Unknown>(oracle.Role!);
        }
    }
}