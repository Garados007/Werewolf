namespace Werewolf.Users
{
    public class DbUserStats
    {
        public uint WinGames { get; set; }

        public uint Killed { get; set; }

        public uint LooseGames { get; set; }

        public uint Leader { get; set; }

        public uint Level { get; set; }

        public ulong CurrentXp { get; set; }

        public DbUserStats() { }

        public DbUserStats(Api.UserStats api)
        {
            WinGames = api.WinGames;
            Killed = api.Killed;
            LooseGames = api.LooseGames;
            Leader = api.Leader;
            Level = api.Level;
            CurrentXp = api.CurrentXp;
        }

        public Api.UserStats ToApi()
        {
            return new Api.UserStats
            {
                WinGames = WinGames,
                Killed = Killed,
                LooseGames = LooseGames,
                Leader = Leader,
                Level = Level,
                CurrentXp = CurrentXp,
            };
        }
    }
}