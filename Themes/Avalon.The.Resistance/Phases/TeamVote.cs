using Werewolf.Theme.Phases;
using Werewolf.Theme;
using System.Collections.Generic;
using System;
using System.Linq;
using Events = Werewolf.Theme.Events;

namespace Avalon.The.Resistance.Phases
{
    public class TeamVote : SeperateVotingPhaseBase<TeamVote.AcceptVoting, BaseRole>
    {
        public class AcceptVoting : Voting
        {
            private readonly ReadOnlyMemory<VoteOption> options;

            public override IEnumerable<(int id, VoteOption option)> Options 
                => options.ToArray().Select((x,i) => (i, x));

            public BaseRole Owner { get; }

            public AcceptVoting(BaseRole owner)
            {
                options = new VoteOption[]
                {
                    new VoteOption("accept"),
                    new VoteOption("deny"),
                };
                Owner = owner;
            }

            public override bool CanView(Role viewer)
                => viewer == Owner;

            public override bool CanVote(Role voter)
                => voter == Owner;

            public override void Execute(GameRoom game, int id)
            {
                if (id == 0)
                    Owner.HasAcceptedRequest = true;
                if (id == 1)
                    Owner.HasAcceptedRequest = false;
                game.SendEvent(new Events.OnRoleInfoChanged(Owner));
            }
        }

        public override bool CanMessage(GameRoom game, Role role)
            => true;

        protected override bool FilterVoter(BaseRole role)
            => true;

        protected override BaseRole GetRole(AcceptVoting voting)
            => voting.Owner;

        protected override AcceptVoting Create(BaseRole role, GameRoom game)
            => new AcceptVoting(role);
    }
}