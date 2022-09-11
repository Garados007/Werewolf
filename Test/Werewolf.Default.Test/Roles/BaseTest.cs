using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Tools;
using Werewolf.Theme.Default;

namespace Werewolf.Default.Test.Roles
{
    using Roles = Werewolf.Theme.Default.Roles;

    [TestClass]
    public class BaseTest
    {
        [TestMethod]
        public async Task VillagerWinFromVillageContest()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Werwolf>(1);
            Assert.AreEqual(4, runner.GameRoom.Users.Count);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // vote wolf and finish voting
            var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
            voting.Vote(room, vill1, wolf);
            voting.Vote(room, vill2, wolf);
            await voting.FinishVotingAsync(room).ConfigureAwait(false);
            await room.NextPhaseAsync().ConfigureAwait(false);

            // now the villager should win the game
            runner.CollectEvent<Theme.Events.GameEnd>();
            room.ExpectWinner(vill1, vill2);
        }

        [TestMethod]
        public async Task WolfWinFromNight()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill = room.GetUserWithRole<Roles.Villager>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // vote villager and finish voting
            var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
            voting.Vote(room, wolf, vill);
            await voting.FinishVotingAsync(room).ConfigureAwait(false);
            await room.NextPhaseAsync().ConfigureAwait(false);

            // now the wolf should win the game
            runner.CollectEvent<Theme.Events.GameEnd>();
            room.ExpectWinner(wolf);
        }

        [TestMethod]
        public async Task MajorPick()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(4)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var vill3 = room.GetUserWithRole<Roles.Villager>(2);
            var vill4 = room.GetUserWithRole<Roles.Villager>(3);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);

            // elect vill1 as major
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.ElectMajorPhase.ElectMajor>();
                voting.Vote(room, vill1, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);
            }

            // create a special voting condition with no clear result
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, vill3);
                voting.Vote(room, vill2, vill3);
                voting.Vote(room, vill3, vill4);
                voting.Vote(room, vill4, vill4);
                voting.Vote(room, wolf, vill2);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
            }

            // the major has to pick now one of the highest results (in this case vill3, vill4)
            {
                room.ExpectNoVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.MajorPick>();
                Assert.IsNull(voting.GetOptionIndex(vill2.User.Id));
                Assert.IsNotNull(voting.GetOptionIndex(vill3.User.Id));
                Assert.IsNotNull(voting.GetOptionIndex(vill4.User.Id));
                voting.Vote(room, vill1, vill3);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
                vill3.Role!.ExpectLiveState(false);
                vill4.Role!.ExpectLiveState(true);
            }
        }

        [TestMethod]
        public async Task MajorInheritDay()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(4)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var vill3 = room.GetUserWithRole<Roles.Villager>(2);
            var vill4 = room.GetUserWithRole<Roles.Villager>(3);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);

            // elect vill1 as major
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.ElectMajorPhase.ElectMajor>();
                voting.Vote(room, vill1, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);
            }

            // kill the major
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill2, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.InheritMajorPhase>().ConfigureAwait(false);
            }

            // the major can now be inherited
            {
                vill1.Role!.ExpectLiveState(true);
                vill1.Role!.ExpectKillFlag<Werewolf.Theme.Default.Effects.KillInfos.VillageKill>();
                var voting = room.ExpectVoting<Theme.Default.Phases.InheritMajorPhase.InheritMajor>();
                voting.Vote(room, vill1, vill3);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
                vill1.Role!.ExpectLiveState(false);
                Assert.IsTrue(vill3.Role!.IsMajor);
            }
        }

        [TestMethod]
        public async Task MajorInheritNight()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(4)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var vill3 = room.GetUserWithRole<Roles.Villager>(2);
            var vill4 = room.GetUserWithRole<Roles.Villager>(3);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);

            // elect vill1 as major
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.ElectMajorPhase.ElectMajor>();
                voting.Vote(room, vill1, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);

            // kill the major
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.InheritMajorPhase>().ConfigureAwait(false);
            }

            // the major can now be inherited
            {
                vill1.Role!.ExpectLiveState(true);
                vill1.Role!.ExpectKillFlag<Werewolf.Theme.Default.Effects.KillInfos.KilledByWerwolf>();
                var voting = room.ExpectVoting<Theme.Default.Phases.InheritMajorPhase.InheritMajor>();
                voting.Vote(room, vill1, vill3);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);
                vill1.Role!.ExpectLiveState(false);
                Assert.IsTrue(vill3.Role!.IsMajor);
            }
        }

        [TestMethod]
        public async Task CheckVisibility()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill = room.GetUserWithRole<Roles.Villager>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);
            
            // verify visibility
            await room.StartGameAsync().ConfigureAwait(false);
            vill.Role!.ExpectVisibility<Roles.Unknown>(wolf.Role!);
            wolf.Role!.ExpectVisibility<Roles.Unknown>(vill.Role!);
        }

        [TestMethod]
        public async Task ElectMajor()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill = room.GetUserWithRole<Roles.Villager>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>();

            // vote villager and finish voting
            var voting = room.ExpectVoting<Theme.Default.Phases.ElectMajorPhase.ElectMajor>();
            voting.Vote(room, wolf, vill);
            await voting.FinishVotingAsync(room).ConfigureAwait(false);
            vill.Role!.ExpectTag(room, "major");
        }
    }
}