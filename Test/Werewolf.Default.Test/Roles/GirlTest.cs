using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Tools;
using Werewolf.Theme.Default;

namespace Werewolf.Default.Test.Roles
{
    using Roles = Werewolf.Theme.Default.Roles;

    [TestClass]
    public class GirlTest
    {
        private static void SetSeed(int? value)
        {
            var type = typeof(Theme.Default.Phases.WerwolfPhase.GirlVote);
            var field = type.GetField("Seed", BindingFlags.Static | BindingFlags.NonPublic);
            if (field is null)
                Assert.Inconclusive("Debug build of tested assembly required");
            else field.SetValue(null, value);
        }

        [TestMethod]
        public async Task GirlSpyNothingHappens()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Girl>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var girl = room.GetUserWithRole<Roles.Girl>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            SetSeed(1000);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // girl vote and select spy
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.GirlVote>();
                voting.Vote(room, girl.User.Id, 1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                wolf.Role!.ExpectVisibility<Roles.Unknown>(girl.Role!);
                girl.Role!.ExpectVisibility<Roles.Unknown>(wolf.Role!);
            }
        }

        [TestMethod]
        public async Task GirlSpyAndGotCatched()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Girl>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var girl = room.GetUserWithRole<Roles.Girl>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            SetSeed(0);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // girl vote and select spy
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.GirlVote>();
                voting.Vote(room, girl.User.Id, 1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                wolf.Role!.ExpectVisibility<Roles.Unknown>(girl.Role!);
                girl.Role!.ExpectVisibility<Roles.Girl>(wolf.Role!);
            }
        }

        [TestMethod]
        public async Task GirlSpyAndSeeWolf()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Girl>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var girl = room.GetUserWithRole<Roles.Girl>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            SetSeed(100);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // girl vote and select spy
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.GirlVote>();
                voting.Vote(room, girl.User.Id, 1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                wolf.Role!.ExpectVisibility<Roles.Werwolf>(girl.Role!);
                girl.Role!.ExpectVisibility<Roles.Unknown>(wolf.Role!);
            }
        }

        [TestMethod]
        public async Task GirlSpyAndGotDetectedAndCanSeeWolf()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Girl>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var girl = room.GetUserWithRole<Roles.Girl>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            SetSeed(10);

            // skip phases until we have our desired one
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // girl vote and select spy
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.GirlVote>();
                voting.Vote(room, girl.User.Id, 1);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                wolf.Role!.ExpectVisibility<Roles.Werwolf>(girl.Role!);
                girl.Role!.ExpectVisibility<Roles.Girl>(wolf.Role!);
            }
        }

        [TestMethod]
        public async Task GirlDoNothing()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Girl>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var girl = room.GetUserWithRole<Roles.Girl>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // skip phases until we have our desired oneselect spy
            await room.StartGameAsync().ConfigureAwait(false);
            room.ExpectPhase<Theme.Default.Phases.WerwolfPhase>();

            // girl vote and select do nothing
            {
                var voting = room.ExpectVoting<Theme.Default.Phases.WerwolfPhase.GirlVote>();
                voting.Vote(room, girl.User.Id, 0);
                await voting.FinishVotingAsync(room).ConfigureAwait(false);
                wolf.Role!.ExpectVisibility<Roles.Unknown>(girl.Role!);
                girl.Role!.ExpectVisibility<Roles.Unknown>(wolf.Role!);
            }
        }

        [TestMethod]
        public async Task GirlLostAbilityAfterOldManDiesFromVillage()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(2)
                .InitRoles<Roles.Girl>(1)
                .InitRoles<Roles.OldMan>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill1 = room.GetUserWithRole<Roles.Villager>(0);
            var vill2 = room.GetUserWithRole<Roles.Villager>(1);
            var girl = room.GetUserWithRole<Roles.Girl>(0);
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
                Assert.IsTrue(((Roles.OldMan)oldman.Role!).WasKilledByVillager);
            }

            // girl has no voting abilities
            room.ExpectNoVoting<Theme.Default.Phases.WerwolfPhase.GirlVote>();
        }

        [TestMethod]
        public async Task CheckVisibility()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.Girl>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill = room.GetUserWithRole<Roles.Villager>(0);
            var girl = room.GetUserWithRole<Roles.Girl>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // verify visibility
            await room.StartGameAsync().ConfigureAwait(false);
            girl.Role!.ExpectVisibility<Roles.Unknown>(wolf.Role!);
            girl.Role!.ExpectVisibility<Roles.Unknown>(vill.Role!);
            wolf.Role!.ExpectVisibility<Roles.Unknown>(girl.Role!);
            vill.Role!.ExpectVisibility<Roles.Unknown>(girl.Role!);
        }
    }
}