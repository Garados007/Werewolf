﻿using Werewolf.User;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Votings;
using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Default.Phases
{
    public class OraclePhase : SingleVotingPhase<OraclePhase.OraclePick>, INightPhase<OraclePhase>
    {
        public class OraclePick : PlayerVotingBase
        {
            public OraclePick(GameRoom game, IEnumerable<UserId>? participants = null)
                : base(game, participants)
            {
            }

            protected override bool DefaultParticipantSelector(Role role)
            {
                return role is BaseRole baseRole &&
                    role.IsAlive && !(role is Roles.Oracle) && !baseRole.IsViewedByOracle;
            }

            public override bool CanView(RoleKind viewer)
            {
                return viewer.IsLeaderOrRole<Roles.Oracle>();
            }

            public override bool CanVote(Role voter)
            {
                return voter is Roles.Oracle && voter.IsAlive;
            }

            public override void Execute(GameRoom game, UserId id, Role role)
            {
                if (role is BaseRole baseRole)
                    baseRole.IsViewedByOracle = true;
            }
        }

        public override bool CanExecute(GameRoom game)
        {
            return game.AliveRoles.Where(x => x is Roles.Oracle).Any() &&
                !game.Users
                    .Select(x => x.Value.Role)
                    .Where(x => x is Roles.OldMan oldMan && oldMan.WasKilledByVillager)
                    .Any();
        }

        protected override OraclePick Create(GameRoom game, IEnumerable<UserId>? ids = null)
            => new OraclePick(game, ids);

        public override bool CanMessage(GameRoom game, RoleKind role)
        {
            return role.IsLeaderOrRole<Roles.Oracle>();
        }
    }
}
