using Werewolf.User;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Votings;
using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Default.Phases
{
    public class FlutistPhase : Phase, INightPhase<FlutistPhase>
    {
        public class FlutistPick : PlayerVotingBase
        {
            public GameRoom Game { get; }

            public FlutistPick(GameRoom game, IEnumerable<UserId>? participants = null)
                : base(game, participants)
            {
                Game = game;
            }

            protected override bool DefaultParticipantSelector(Role role)
            {
                return role is BaseRole baseRole && baseRole.IsAlive &&
                    !baseRole.IsEnchantedByFlutist && !(role is Roles.Flutist);
            }

            public override bool CanView(Role viewer)
            {
                return viewer is Roles.Flutist;
            }

            public override bool CanVote(Role voter)
            {
                return voter is Roles.Flutist && voter.IsAlive;
            }

            public override void Execute(GameRoom game, UserId id, Role role)
            {
                if (role is not BaseRole baseRole)
                    return;
                baseRole.IsEnchantedByFlutist = true;
                if (game.Phase?.Current is FlutistPhase pick)
                {
                    pick.VotingFinished(this);
                }
            }

            public override void RemoveOption(UserId user)
            {
                base.RemoveOption(user);

                if (OptionsDict.IsEmpty)
                    CheckVotingFinished(Game);
            }
        }

        public override bool CanExecute(GameRoom game)
        {
            return game.AliveRoles.Any(x => x is Roles.Flutist);
        }

        private readonly List<FlutistPick> picks = new List<FlutistPick>();

        protected override void Init(GameRoom game)
        {
            base.Init(game);
            picks.Clear();
            int maxEnchants = game.Users
                .Select(x => x.Value.Role)
                .Where(x => x is not null)
                .Count() 
                < 8 ? 1 : 2;
            for (int i = 0; i < maxEnchants; ++i)
            {
                var pick = new FlutistPick(game);
                picks.Add(pick);
                AddVoting(pick);
            }
        }

        public void VotingFinished(FlutistPick voting)
        {
            var result = voting.GetResultUserIds().ToArray();
            if (result.Length == 1)
            {
                foreach (var other in picks)
                    if (other != voting)
                        other.RemoveOption(result[0]);
            }
        }

        public override void ExecuteMultipleWinner(Voting voting, GameRoom game)
        {
            var index = picks.FindIndex(x => x == voting);
            if (index < 0)
                return;
            var ids = picks[index].GetResultUserIds().ToArray();
            if (ids.Length > 0)
                AddVoting(picks[index] = new FlutistPick(game, ids));
            RemoveVoting(voting);
        }

        public override bool CanMessage(GameRoom game, Role role)
        {
            return role is Roles.Flutist;
        }
    }
}
