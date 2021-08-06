using Werewolf.User;
using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Phases
{
    public class KillTransitionToBeforeKillAction : ActionPhaseBase
    {
        public override void Execute(GameRoom game)
        {
            var dict = new Dictionary<string, HashSet<UserId>>();
            bool doExecute = true;
            while (doExecute)
            {
                doExecute = false;
                foreach (var (id, entry) in game.Users)
                    if (entry.Role != null && entry.Role.KillState == KillState.AboutToKill)
                    {
                        entry.Role.ChangeToBeforeKill(game);
                        var lid = entry.Role.KillInfo?.NotificationId ?? "";
                        if (!dict.TryGetValue(lid, out HashSet<UserId>? set))
                            dict.Add(lid, set = new HashSet<UserId>());
                        _ = set.Add(id);
                        doExecute = true;
                    }
                if (doExecute)
                {
                    foreach (var role in game.Users.Select(x => x.Value.Role))
                        if (role != null && role.KillState == KillState.MarkedKill)
                        {
                            role.ChangeToAboutToKill(game);
                        }
                }
            }
            if (dict.Count == 1)
            {
                var first = dict.First();
                game.SendEvent(new Events.PlayerNotification(first.Key, first.Value.ToArray()));
            }
            if (dict.Count > 1)
            {
                game.SendEvent(new Events.MultiPlayerNotification(dict));
            }
        }
    }
}
