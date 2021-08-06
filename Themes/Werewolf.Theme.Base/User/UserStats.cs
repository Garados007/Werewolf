using System;
using System.Threading.Tasks;

namespace Werewolf.User
{
    public abstract class UserStats
    {
        public abstract uint WinGames { get; }

        public abstract uint Killed { get; }

        public abstract uint LooseGames { get; }

        public abstract uint Leader { get; }

        public abstract uint Level { get; }

        public abstract ulong CurrentXp { get; }
        
        public ulong LevelMaxXP
            => (ulong)(40 * (Math.Pow(Level, 1.2) + Math.Pow(1.1, Math.Pow(Level, 0.5))));

        public abstract Task IncAsync(
            uint dWinGames,
            uint dKilled,
            uint dLooseGames,
            uint dLeader,
            ulong dXp
        );
    }
}