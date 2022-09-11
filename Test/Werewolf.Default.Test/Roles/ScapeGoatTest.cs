using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Tools;
using Werewolf.Theme.Default;

namespace Werewolf.Default.Test.Roles
{
    using Roles = Werewolf.Theme.Default.Roles;

    [TestClass]
    public class ScapeGoatTest09
    {
        [TestMethod]
        public async Task ScapeGoatKilledAtDayAndSelectsSome()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(3)
                .InitRoles<Roles.ScapeGoat>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var vill3 = room.GetUserWithRole<Roles.Villager>(2);
            var goat = room.GetUserWithRole<Roles.ScapeGoat>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();

            // create a tie and therefore kill the scapegoat
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, vill2);
                voting.Vote(room, vill2, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ScapeGoatPhase>().ConfigureAwait(false);
            }

            // scape goat selects some
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.ScapeGoatPhase.ScapeGoatSelect>();
                Assert.IsNull(voting.Vote(room, goat, vill1));
                Assert.IsNull(voting.Vote(room, goat, vill3));
                Assert.IsNull(voting.Vote(room, goat.User.Id, 0));
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
                goat.Role!.ExpectLiveState(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();

            // only selected one is able to vote this time
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                Assert.IsTrue(voting.CanVote(vill1.Role!));
                Assert.IsFalse(voting.CanVote(vill2.Role!));
                Assert.IsTrue(voting.CanVote(vill3.Role!));
                Assert.IsFalse(voting.CanVote(wolf.Role!));
                voting.Vote(room, vill1, vill2);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
                vill2.Role!.ExpectLiveState(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();

            // everyone can vote again
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                Assert.IsTrue(voting.CanVote(vill1.Role!));
                Assert.IsTrue(voting.CanVote(vill3.Role!));
                Assert.IsTrue(voting.CanVote(wolf.Role!));
            }
        }

        [TestMethod]
        public async Task ScapeGoatKilledAtDayAndSelectsNoone()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.ScapeGoat>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var goat = room.GetUserWithRole<Roles.ScapeGoat>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();

            // create a tie and therefore kill the scapegoat
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, vill2);
                voting.Vote(room, vill2, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ScapeGoatPhase>().ConfigureAwait(false);
            }

            // scape goat selects noone
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.ScapeGoatPhase.ScapeGoatSelect>();
                Assert.IsNull(voting.Vote(room, goat.User.Id, 0));
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
                goat.Role!.ExpectLiveState(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();

            // noone is able to vote
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                Assert.IsFalse(voting.CanVote(vill1.Role!));
                Assert.IsFalse(voting.CanVote(vill2.Role!));
                Assert.IsFalse(voting.CanVote(wolf.Role!));
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();

            // everyone can vote again
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                Assert.IsTrue(voting.CanVote(vill1.Role!));
                Assert.IsTrue(voting.CanVote(vill2.Role!));
                Assert.IsTrue(voting.CanVote(wolf.Role!));
            }
        }

        [TestMethod]
        public async Task ScapeGoatKilledAtDayAndSelectOneButGotKilled()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.ScapeGoat>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var goat = room.GetUserWithRole<Roles.ScapeGoat>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();

            // create a tie and therefore kill the scapegoat
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, vill2);
                voting.Vote(room, vill2, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ScapeGoatPhase>().ConfigureAwait(false);
            }

            // scape goat selects some
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.ScapeGoatPhase.ScapeGoatSelect>();
                Assert.IsNull(voting.Vote(room, goat, vill1));
                Assert.IsNull(voting.Vote(room, goat.User.Id, 0));
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
                goat.Role!.ExpectLiveState(false);
            }

            // wolf kills voter
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>();
                vill1.Role!.ExpectLiveState(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();

            // noone is able to vote, because the only one got killed
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                Assert.IsFalse(voting.CanVote(vill2.Role!));
                Assert.IsFalse(voting.CanVote(wolf.Role!));
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();

            // everyone can vote again
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                Assert.IsTrue(voting.CanVote(vill2.Role!));
                Assert.IsTrue(voting.CanVote(wolf.Role!));
            }
        }

        [TestMethod]
        public async Task ScapeGoatLostAbilityAfterOldManDiesFromVillage()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.ScapeGoat>(1)
                .InitRoles<Roles.OldMan>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var goat = room.GetUserWithRole<Roles.ScapeGoat>(0);
            var oldman = room.GetUserWithRole<Roles.OldMan>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // old man dies
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, oldman);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // create a tie, scape goat will no longer die
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, vill2);
                voting.Vote(room, vill2, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                // scape goat was not killed and there is no special phase
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
                goat.Role!.ExpectLiveState(true);
            }
        }

        [TestMethod]
        public async Task CheckVisibility()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.ScapeGoat>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill = room.GetUserWithRole<Roles.Villager>(0);
            var scapegoat = room.GetUserWithRole<Roles.ScapeGoat>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // verify visibility
            await room.StartGameAsync().ConfigureAwait(false);
            scapegoat.Role!.ExpectVisibility<Roles.Unknown>(wolf.Role!);
            scapegoat.Role!.ExpectVisibility<Roles.Unknown>(vill.Role!);
            wolf.Role!.ExpectVisibility<Roles.Unknown>(scapegoat.Role!);
            vill.Role!.ExpectVisibility<Roles.Unknown>(scapegoat.Role!);
        }
    }
}