using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Tools;
using Werewolf.Theme.Default;

namespace Werewolf.Default.Test.Roles
{
    using Roles = Werewolf.Theme.Default.Roles;

    [TestClass]
    public class WitchTest
    {
        [TestMethod]
        public async Task WitchProtectsVictim()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Witch>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var witch = room.GetUserWithRole<Roles.Witch>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // wolf voting and kill villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WitchPhase>().ConfigureAwait(false);
            }

            // witch protects victim
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WitchPhase.WitchSafe>();
                voting.Vote(room, witch, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
                vill1.Role!.ExpectLiveState(true);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>();

            // wolf voting and kill villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WitchPhase>().ConfigureAwait(false);
            }

            // witch has no longer the ability to protect
            room.ExpectNoVoting<Theme.Default.Phases.WitchPhase.WitchSafe>();
        }

        [TestMethod]
        public async Task WitchDoesntProtectVictim()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Witch>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var witch = room.GetUserWithRole<Roles.Witch>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // wolf voting and kill villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WitchPhase>().ConfigureAwait(false);
            }

            // witch doesn't protect victim
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WitchPhase.WitchSafe>();
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
                vill1.Role!.ExpectLiveState(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>();

            // wolf voting and kill villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill2);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WitchPhase>().ConfigureAwait(false);
            }

            // witch has the ability to protect now
            room.ExpectVoting<Theme.Default.Phases.WitchPhase.WitchSafe>();
        }

        [TestMethod]
        public async Task WitchKillsAnotherOne()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(3)
                .InitRoles<Roles.Witch>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var vill3 = room.GetUserWithRole<Roles.Villager>(2);
            var witch = room.GetUserWithRole<Roles.Witch>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // wolf voting and kill villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WitchPhase>().ConfigureAwait(false);
            }

            // witch kills other player
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WitchPhase.WitchKill>();
                voting.Vote(room, witch, vill2);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
                vill1.Role!.ExpectLiveState(false);
                vill2.Role!.ExpectLiveState(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>();

            // wolf voting and kill villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill3);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WitchPhase>().ConfigureAwait(false);
            }

            // witch has no longer the ability to kill another one
            room.ExpectNoVoting<Theme.Default.Phases.WitchPhase.WitchKill>();
        }


        [TestMethod]
        public async Task WitchDoesntKillAnotherOne()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(3)
                .InitRoles<Roles.Witch>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var vill3 = room.GetUserWithRole<Roles.Villager>(2);
            var witch = room.GetUserWithRole<Roles.Witch>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // wolf voting and kill villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WitchPhase>().ConfigureAwait(false);
            }

            // witch doesn't kill other player
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WitchPhase.WitchKill>();
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
                vill1.Role!.ExpectLiveState(false);
            }

            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>();

            // wolf voting and kill villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill3);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WitchPhase>().ConfigureAwait(false);
            }

            // witch can kill another one again
            room.ExpectVoting<Theme.Default.Phases.WitchPhase.WitchKill>();
        }

        [TestMethod]
        public async Task WitchKillsAndWin()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.Witch>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var witch = room.GetUserWithRole<Roles.Witch>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // wolf voting and kill villager
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WitchPhase>().ConfigureAwait(false);
            }

            // witch kills wolf
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WitchPhase.WitchKill>();
                voting.Vote(room, witch, wolf);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.NextPhaseAsync().ConfigureAwait(false);
                room.ExpectWinner(witch, vill1);
            }
        }

        [TestMethod]
        public async Task WitchKillsAndLoose()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.Witch>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var witch = room.GetUserWithRole<Roles.Witch>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // wolf voting and kill witch
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.WerwolfVote>();
                voting.Vote(room, wolf, witch);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WitchPhase>().ConfigureAwait(false);
            }

            // witch kills wolf
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WitchPhase.WitchKill>();
                voting.Vote(room, witch, vill1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.NextPhaseAsync().ConfigureAwait(false);
                room.ExpectWinner(wolf);
            }
        }


        [TestMethod]
        public async Task WitchLostAbilityAfterOldManDiesFromVillage()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Witch>(1)
                .InitRoles<Roles.OldMan>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var witch = room.GetUserWithRole<Roles.Witch>(0);
            var oldman = room.GetUserWithRole<Roles.OldMan>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired oneselect spy
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.WitchPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.DailyVictimElectionPhase>().ConfigureAwait(false);

            // old man dies
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.DailyVictimElectionPhase.DailyVote>();
                voting.Vote(room, vill1, oldman);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                await room.ExpectNextPhaseAsync<Theme.Default.Phases.WerwolfPhase>().ConfigureAwait(false);
            }

            // witch has no special phase anymore
            await room.ExpectNextPhaseAsync<Theme.Default.Phases.ElectMajorPhase>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CheckVisibility()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.Witch>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill = room.GetUserWithRole<Roles.Villager>(0);
            var witch = room.GetUserWithRole<Roles.Witch>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // verify visibility
            await room.StartGameAsync().ConfigureAwait(false);
            witch.Role!.ExpectVisibility<Roles.Unknown>(wolf.Role!);
            witch.Role!.ExpectVisibility<Roles.Unknown>(vill.Role!);
            wolf.Role!.ExpectVisibility<Roles.Unknown>(witch.Role!);
            vill.Role!.ExpectVisibility<Roles.Unknown>(witch.Role!);
        }
    }
}