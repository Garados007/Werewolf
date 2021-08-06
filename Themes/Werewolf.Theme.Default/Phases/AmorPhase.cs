using Werewolf.User;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Votings;
using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Default.Phases
{
    public class AmorPhase : Phase, INightPhase<AmorPhase>
    {
        public class AmorPick : PlayerVotingBase
        {
            public AmorPick(GameRoom game, IEnumerable<UserId>? participants = null)
                : base(game, participants)
            {
            }

            public override bool CanView(Role viewer)
            {
                return viewer is Roles.Amor;
            }

            public override bool CanVote(Role voter)
            {
                return voter is Roles.Amor && voter.IsAlive;
            }

            public override void Execute(GameRoom game, UserId id, Role role)
            {
                if (role is not BaseRole baseRole)
                    return;
                baseRole.IsLoved = true;
                foreach (var other in game.Users.Select(x => x.Value.Role))
                    if (role != other && other is BaseRole otherBase && otherBase.IsLoved)
                        game.SendEvent(new Events.OnRoleInfoChanged(other, target: id));
                if (game.Phase?.Current is AmorPhase pick)
                {
                    pick.VotingFinished(this);
                }
            }
        }

        public override bool CanExecute(GameRoom game)
        {
            return game.AliveRoles.Any(x => x is Roles.Amor) &&
                !game.Users
                    .Select(x => x.Value.Role)
                    .Where(x => x is BaseRole role && role.IsLoved)
                    .Any();
        }

        private AmorPick? pick1, pick2;

        protected override void Init(GameRoom game)
        {
            base.Init(game);
            AddVoting(pick1 = new AmorPick(game));
            AddVoting(pick2 = new AmorPick(game));
        }

        public void VotingFinished(AmorPick voting)
        {
            var result = voting.GetResultUserIds().ToArray();
            if (result.Length == 1)
            {
                if (pick1 == voting)
                    pick2?.RemoveOption(result[0]);
                if (pick2 == voting)
                    pick1?.RemoveOption(result[0]);
            }
        }

        public override void ExecuteMultipleWinner(Voting voting, GameRoom game)
        {
            if (voting == pick1)
            {
                var ids = pick1.GetResultUserIds().ToArray();
                if (ids.Length > 0)
                    AddVoting(pick1 = new AmorPick(game, ids));
                RemoveVoting(voting);
            }
            if (voting == pick2)
            {
                var ids = pick2.GetResultUserIds().ToArray();
                if (ids.Length > 0)
                    AddVoting(pick2 = new AmorPick(game, ids));
                RemoveVoting(voting);
            }
        }

        public override bool CanMessage(GameRoom game, Role role)
        {
            return role is Roles.Amor;
        }
    }
}
