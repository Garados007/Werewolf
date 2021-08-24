using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneOf.Types;
using Test.Tools;
using Werewolf.Theme.Default;

namespace Werewolf.Default.Test.Roles
{
    using Roles = Werewolf.Theme.Default.Roles;

    [TestClass]
    public class FlutistTest
    {
        [TestMethod]
        public async Task FlutistCannotWinWithVillager()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Flutist>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var flutist = room.GetUserWithRole<Roles.Flutist>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // wolf voting and kill villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.FlutistPhase>().ConfigureAwait(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // village kills wolf
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill2, wolf);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.FlutistPhase>().ConfigureAwait(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // village kills flutist
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill2, flutist);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.NextPhaseAsync().ConfigureAwait(false);
                room.ExpectWinner(vill1, vill2);
            }
        }

        [TestMethod]
        public async Task FlutistCannotWinWithWolf()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Flutist>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var flutist = room.GetUserWithRole<Roles.Flutist>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // wolf voting and kill villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.FlutistPhase>().ConfigureAwait(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // village kills villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill2, vill2);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.FlutistPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // village kills flutist
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, wolf, flutist);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.NextPhaseAsync().ConfigureAwait(false);
                room.ExpectWinner(wolf);
            }
        }

        [TestMethod]
        public async Task FlutistCannotWinsAlone()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Flutist>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var flutist = room.GetUserWithRole<Roles.Flutist>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // wolf voting and kill villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.FlutistPhase>().ConfigureAwait(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // village kills wolf
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill2, wolf);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.FlutistPhase>().ConfigureAwait(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // village kills villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, flutist, vill2);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.NextPhaseAsync().ConfigureAwait(false);
                room.ExpectWinner(flutist);
            }
        }

        [TestMethod]
        public async Task FlutistWinWithEncantedTogether()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.Flutist>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var flutist = room.GetUserWithRole<Roles.Flutist>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.FlutistPhase>().ConfigureAwait(false);

            // flutist votes villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.FlutistPhase.FlutistPick>();
                Assert.AreEqual(null, voting.Vote(room, flutist, vill1));
                await voting.FinishVotingAsync(room).ConfigureAwait(false);

                room.ExpectNoVoting<Theme.Default.Phases.FlutistPhase.FlutistPick>();

                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
                vill1.Role!.ExpectTag(room, "enchant-flutist");
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // village kills villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, flutist, wolf);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.NextPhaseAsync().ConfigureAwait(false);
                room.ExpectWinner(flutist);
            }
        }

        [TestMethod]
        public async Task FlutistHasTwoVotesInLargeGroups()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(10)
                .InitRoles<Roles.Flutist>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var flutist = room.GetUserWithRole<Roles.Flutist>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.FlutistPhase>().ConfigureAwait(false);

            // flutist votes villager and wolf
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.FlutistPhase.FlutistPick>();
                Assert.AreEqual(null, voting.Vote(room, flutist, vill1));
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                
                voting = room.ExpectVoting<Theme.Default.Phases.FlutistPhase.FlutistPick>();
                Assert.AreEqual(null, voting.Vote(room, flutist, vill2));
                await voting.FinishVotingAsync(room).ConfigureAwait(false);

                room.ExpectNoVoting<Theme.Default.Phases.FlutistPhase.FlutistPick>();

                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
                vill1.Role!.ExpectTag(room, "enchant-flutist");
                vill2.Role!.ExpectTag(room, "enchant-flutist");
            }
        }


        [TestMethod]
        public async Task DeadFlutistCanWinIfOnlyEnchanted()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.Flutist>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var flutist = room.GetUserWithRole<Roles.Flutist>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.FlutistPhase>().ConfigureAwait(false);

            // flutist votes wolf
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.FlutistPhase.FlutistPick>();
                voting.Vote(room, flutist, wolf);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // village kills flutist
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, flutist);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            }

            // wolf kills villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.NextPhaseAsync().ConfigureAwait(false);
                room.ExpectWinner(flutist);
            }
        }

        [TestMethod]
        public async Task CheckVisibility()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.Flutist>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill = room.GetUserWithRole<Roles.Villager>(0);
            var flutist = room.GetUserWithRole<Roles.Flutist>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // verify visibility
            await room.StartGameAsync().ConfigureAwait(false);
            flutist.Role!.ExpectVisibility<Roles.Unknown>(wolf.Role!);
            flutist.Role!.ExpectVisibility<Roles.Unknown>(vill.Role!);
            wolf.Role!.ExpectVisibility<Roles.Unknown>(flutist.Role!);
            vill.Role!.ExpectVisibility<Roles.Unknown>(flutist.Role!);
        }
    }
}