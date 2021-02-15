using System.Collections.Generic;

namespace Werewolf.Theme
{
    public abstract class KillInfo
    {
        public virtual string NotificationId
        {
            get
            {
                var name = GetType().FullName ?? "";
                var ind = name.LastIndexOf('.');
                return ind < 0 ? name : name[(ind + 1)..];
            }
        }

        public abstract IEnumerable<string> GetKillFlags(GameRoom game, Role? viewer);

    }
}
