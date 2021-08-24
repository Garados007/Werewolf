using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Tools;
using Werewolf.Theme.Default;

namespace Werewolf.Default.Test.Roles
{
    using Roles = Werewolf.Theme.Default.Roles;

    [TestClass]
    public class PureSoulTest
    {
        [TestMethod]
        public async Task CheckVisibility()
        {
            // create runner and fill with data
            var runner = new Runner<DefaultTheme>()
                .InitRoles<Roles.Villager>(1)
                .InitRoles<Roles.PureSoul>(1)
                .InitRoles<Roles.Werwolf>(1);
            var room = runner.GameRoom;
            var vill = room.GetUserWithRole<Roles.Villager>(0);
            var puresoul = room.GetUserWithRole<Roles.PureSoul>(0);
            var wolf = room.GetUserWithRole<Roles.Werwolf>(0);

            // verify visibility
            await room.StartGameAsync().ConfigureAwait(false);
            puresoul.Role!.ExpectVisibility<Roles.PureSoul>(wolf.Role!);
            puresoul.Role!.ExpectVisibility<Roles.PureSoul>(vill.Role!);
            wolf.Role!.ExpectVisibility<Roles.Unknown>(puresoul.Role!);
            vill.Role!.ExpectVisibility<Roles.Unknown>(puresoul.Role!);
        }
    }
}