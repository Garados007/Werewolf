using Werewolf.Theme.Votings;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Users.Api;

namespace Werewolf.Theme.Phases
{
    public abstract class SingleVotingPhase<T> : Phase
        where T : PlayerVotingBase
    {
        protected abstract T Create(GameRoom game, IEnumerable<UserId>? ids = null);

        protected override void Init(GameRoom game)
        {
            base.Init(game);
            AddVoting(Create(game));
        }

        public override void ExecuteMultipleWinner(Voting voting, GameRoom game)
        {
            if (voting is T tvoting)
            {
                var ids = tvoting.GetResultUserIds().ToArray();
                if (ids.Length > 2)
                    AddVoting(Create(game, ids));
                else if (ids.Length == 1 && game.Participants.TryGetValue(ids[0], out Role? role) && role != null)
                    tvoting.Execute(game, ids[0], role);
                RemoveVoting(tvoting);
            }
        }
    }
}
