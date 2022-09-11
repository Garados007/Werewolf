using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Tools;
using Werewolf.Theme.Default;

namespace Werewolf.Default.Test.Roles
{
    using Roles = Werewolf.Theme.Default.Roles;

    [TestClass]
    public class IdiotTest
    {
        [TestMethod]
        public async Task VillageCannotKillIdiot()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Idiot>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var idiot = room.GetUserWithRole<Roles.Idiot>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();

            // village tries to kill the first time
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                Assert.IsTrue(voting.CanVote(idiot.Role!));
                voting.Vote(room, vill1, idiot);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
                idiot.Role!.ExpectLiveState(true);
                idiot.Role!.ExpectVisibility<Roles.Idiot>(vill1.Role!);
                idiot.Role!.ExpectVisibility<Roles.Idiot>(vill2.Role!);
                idiot.Role!.ExpectVisibility<Roles.Idiot>(wolf.Role!);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();

            // village cannot mark idiot as target, idiot cannot vote any longer
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                Assert.ThrowsException<KeyNotFoundException>(() => voting.Vote(room, vill1, idiot));
                Assert.ThrowsException<KeyNotFoundException>(() => voting.Vote(room, wolf, idiot));
                Assert.IsFalse(voting.CanVote(idiot.Role!));
            }
        }

        [TestMethod]
        public async Task WolfesCanKillIdiot()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Idiot>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var idiot = room.GetUserWithRole<Roles.Idiot>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // wolf kills idiot
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, idiot);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
                idiot.Role!.ExpectLiveState(false);
            }
        }

        [TestMethod]
        public async Task WolfesCanKillVisibleIdiot()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Idiot>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var idiot = room.GetUserWithRole<Roles.Idiot>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // village tries to kill idiot
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, idiot);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            }

            // wolf kills idiot
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, idiot);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
                idiot.Role!.ExpectLiveState(false);
            }
        }

        [TestMethod]
        public async Task WitchCanKillIdiot()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Idiot>(1)
                .InitRoles<Roles.Witch>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var idiot = room.GetUserWithRole<Roles.Idiot>(0);
            var witch = room.GetUserWithRole<Roles.Witch>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.WitchPhase>().ConfigureAwait(false);

            // witch kills idiot
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WitchPhase.WitchKill>();
                voting.Vote(room, witch, idiot);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
                idiot.Role!.ExpectLiveState(false);
            }
        }

        [TestMethod]
        public async Task WitchCanKillVisibleIdiot()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Idiot>(1)
                .InitRoles<Roles.Witch>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var idiot = room.GetUserWithRole<Roles.Idiot>(0);
            var witch = room.GetUserWithRole<Roles.Witch>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.WitchPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // village tries to kill idiot
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, idiot);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WitchPhase>().ConfigureAwait(false);
            }

            // witch kills idiot
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WitchPhase.WitchKill>();
                voting.Vote(room, witch, idiot);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
                idiot.Role!.ExpectLiveState(false);
            }
        }

        [TestMethod]
        public async Task HunterCanKillIdiot()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Idiot>(1)
                .InitRoles<Roles.Hunter>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var idiot = room.GetUserWithRole<Roles.Idiot>(0);
            var hunter = room.GetUserWithRole<Roles.Hunter>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // wolf kills hunter
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, hunter);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.HunterPhase>();
            }

            // hunter kills idiot
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.HunterPhase.HunterKill>();
                voting.Vote(room, hunter, idiot);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
                idiot.Role!.ExpectLiveState(false);
            }
        }

        [TestMethod]
        public async Task HunterCanKillVisibleIdiot()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Idiot>(1)
                .InitRoles<Roles.Hunter>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var idiot = room.GetUserWithRole<Roles.Idiot>(0);
            var hunter = room.GetUserWithRole<Roles.Hunter>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // village tries to kill idiot
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, idiot);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            }

            // wolf kills hunter
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, hunter);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.HunterPhase>().ConfigureAwait(false);
            }

            // hunter kills idiot
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.HunterPhase.HunterKill>();
                voting.Vote(room, hunter, idiot);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
                idiot.Role!.ExpectLiveState(false);
            }
        }

        [TestMethod]
        public async Task RevealedIdiotCanDiscardMajor()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Idiot>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var idiot = room.GetUserWithRole<Roles.Idiot>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);

            // the idiot becomes the new major
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.ElectMajorPhase.ElectMajor>();
                voting.Vote(room, vill1, idiot);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);
            }

            // village tries to kill idiot
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, idiot);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            }
            
            // they should be no new major selection phase
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // and no major pick will happen again
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, vill2);
                voting.Vote(room, vill2, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                room.ExpectNoVoting<Theme.Default.Phases.DailyVictimElectionPhase.MajorPick>();
            }

            // and the major role is never transmitted to the next player
            {
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, idiot);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task CheckVisibility()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.Idiot>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill = room.GetUserWithRole<Roles.Villager>(0);
            var idiot = room.GetUserWithRole<Roles.Idiot>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // verify visibility
            await room.StartGameAsync().ConfigureAwait(false);
            idiot.Role!.ExpectVisibility<Roles.Unknown>(wolf.Role!);
            idiot.Role!.ExpectVisibility<Roles.Unknown>(vill.Role!);
            wolf.Role!.ExpectVisibility<Roles.Unknown>(idiot.Role!);
            vill.Role!.ExpectVisibility<Roles.Unknown>(idiot.Role!);
        }
    }
}