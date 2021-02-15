using System;

namespace Werewolf.Users.Api
{
    partial class UserStats
    {
        public ulong LevelMaxXP 
            => (ulong)(40 * (Math.Pow(Level, 1.2) + Math.Pow(1.1, Math.Pow(Level, 0.5))));
    }
}