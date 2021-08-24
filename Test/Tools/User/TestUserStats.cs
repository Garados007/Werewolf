using System.Threading.Tasks;

namespace Test.Tools.User
{
    public class TestUserStats : Werewolf.User.UserStats
    {
        public override uint WinGames => 0;

        public override uint Killed => 0;

        public override uint LooseGames => 0;

        public override uint Leader => 0;

        public override uint Level => 0;

        public override ulong CurrentXp => 0;

        public override Task IncAsync(uint dWinGames, uint dKilled, uint dLooseGames, uint dLeader, ulong dXp)
        {
            return Task.CompletedTask;
        }
    }
}