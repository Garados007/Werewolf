using Werewolf.User;
using System;

namespace Werewolf.Theme
{
    public class GameUserEntry
    {
        public Role? Role { get; set; }

        public UserInfo User { get; set; }

        public bool IsOnline => connections > 0;

        private int connections;
        private readonly object connectionLock = new object();
        
        public DateTime LastConnectionUpdate { get; private set; }

        public GameUserEntry(UserInfo user)
        {
            User = user;
        }

        public void AddConnection()
        {
            lock (connectionLock)
            {
                connections++;
                LastConnectionUpdate = DateTime.UtcNow;
            }
        }

        public void RemoveConnection()
        {
            lock (connectionLock)
            {
                connections = Math.Max(0, connections - 1);
                LastConnectionUpdate = DateTime.UtcNow;
            }
        }
    }
}