using Werewolf.User;
using System;

namespace Werewolf.Theme
{
    public class GameUserEntry
    {
        public Role? Role { get; set; }

        public UserInfo User { get; set; }

        public int ConnectionChanged { get; private set; }

        public bool IsOnline => connections > 0;

        public bool WasOnlineOnce { get; private set; }

        private int connections;
        private readonly object connectionLock = new object();
        
        public DateTime LastConnectionUpdate { get; private set; }


        public Effects.EffectCollection<Effects.IGameUserEntryEffect> Effects { get; } = new();

        public GameUserEntry(UserInfo user)
        {
            User = user;
        }

        public void AddConnection()
        {
            lock (connectionLock)
            {
                connections++;
                ConnectionChanged++;
                LastConnectionUpdate = DateTime.UtcNow;
                WasOnlineOnce = true;
            }
        }

        public void RemoveConnection()
        {
            lock (connectionLock)
            {
                connections = Math.Max(0, connections - 1);
                ConnectionChanged++;
                LastConnectionUpdate = DateTime.UtcNow;
            }
        }
    }
}