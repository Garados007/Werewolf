using Werewolf.Users.Api;
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
                foreach (var (id, role) in game.Participants)
                    if (role != null && role.KillState == KillState.AboutToKill)
                    {
                        role.ChangeToBeforeKill(game);
                        var lid = role.KillInfo?.NotificationId ?? "";
                        if (!dict.TryGetValue(lid, out HashSet<UserId>? set))
                            dict.Add(lid, set = new HashSet<UserId>());
                        set.Add(id);
                        doExecute = true;
                    }
                if (doExecute)
                {
                    foreach (var role in game.Participants.Values)
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
