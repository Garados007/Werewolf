using System.Collections.Generic;
using System.Linq;
using Werewolf.Theme;
using Werewolf.Theme.Phases;
using Werewolf.Users.Api;
using Events = Werewolf.Theme.Events;

namespace Avalon.The.Resistance.Phases
{
    public class EvaluationAction : ActionPhaseBase
    {
        public override void Execute(GameRoom game)
        {
            // check the vote results
            var accepted = new List<(UserId id, BaseRole role)>();
            var denied = new List<(UserId id, BaseRole role)>();
            foreach (var (id, role) in game.Participants)
                if (role is BaseRole baseRole)
                {
                    if (baseRole.HasAcceptedRequest == true)
                        accepted.Add((id, baseRole));
                    if (baseRole.HasAcceptedRequest == false)
                        denied.Add((id, baseRole));
                }
            // send message
            if (accepted.Count > 0)
                game.SendEvent(new Events.PlayerNotification(
                    "accepted-player",
                    accepted.Select(x => x.id).ToArray()
                ));
            if (denied.Count > 0)
                game.SendEvent(new Events.PlayerNotification(
                    "denied-player",
                    denied.Select(x => x.id).ToArray()
                ));
            // action
        }
    }
}