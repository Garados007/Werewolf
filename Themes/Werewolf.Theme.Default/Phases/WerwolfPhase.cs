using Werewolf.Users.Api;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Default.Roles;
using Werewolf.Theme.Votings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Default.Phases
{
    public class WerwolfPhase : MultiPhase<WerwolfPhase.WerwolfVotingPhase, WerwolfPhase.GirlVotePhase>, INightPhase<WerwolfPhase>
    {
        public class WerwolfVote : PlayerVotingBase
        {
            public WerwolfVote(GameRoom game, IEnumerable<UserId>? participants = null)
                : base(game, participants)
            {
            }

            protected override bool DefaultParticipantSelector(Role role)
            {
                return !(role is WerwolfBase) && role.IsAlive;
            }

            public override bool CanView(Role viewer)
            {
                return viewer is WerwolfBase;
            }

            public override bool CanVote(Role voter)
            {
                return voter is WerwolfBase && voter.IsAlive;
            }

            public override void Execute(GameRoom game, UserId id, Role role)
            {
                role.AddKillFlag(new KillInfos.KilledByWerwolf());
            }
        }

        public class WerwolfVotingPhase : SingleVotingPhase<WerwolfVote>
        {
            public GirlVotePhase? GirlVotingPhase { get; set; }

            public override void RemoveVoting(Voting voting)
            {
                base.RemoveVoting(voting);
                if (!Votings.Any() && GirlVotingPhase != null)
                    foreach (var v in GirlVotingPhase.Votings)
                        GirlVotingPhase.RemoveVoting(v);
            }

            public override bool CanExecute(GameRoom game)
            {
                return game.AliveRoles.Where(x => x is WerwolfBase).Any();
            }

            protected override WerwolfVote Create(GameRoom game, IEnumerable<UserId>? ids = null)
                => new WerwolfVote(game, ids);

            public override bool CanMessage(GameRoom game, Role role)
            {
                return role is WerwolfBase;
            }
        }

        public class GirlVote : Voting
        {
            private readonly List<VoteOption> options = new List<VoteOption>();
            public override IEnumerable<(int id, VoteOption option)> Options
                => options.Select((opt, id) => (id, opt));

            public Girl Girl { get; }

            public GirlVote(Girl girl)
            {
                Girl = girl;
                options.Add(new VoteOption("do-nothing"));
                options.Add(new VoteOption("spy"));
            }

            public override bool CanView(Role viewer)
                => viewer == Girl;

            public override bool CanVote(Role voter)
                => voter == Girl;

            public override void Execute(GameRoom game, int id)
            {
                if (id != 1)
                    return;
                var rng = new Random();
                int wolfCount = game.AliveRoles.Where(x => x is WerwolfBase).Count();
                int aliveCount = game.AliveRoles.Count();
                var probabilitySeeWolf = (double)wolfCount / aliveCount;
                var probabilitySeeGirl = 1.0 / aliveCount;

                foreach (var wolf in game.AliveRoles.Where(x => x is WerwolfBase).Cast<WerwolfBase>())
                {
                    if (probabilitySeeWolf >= 1 - rng.NextDouble())
                        wolf.AddSeenByGirl(Girl);
                    if (probabilitySeeGirl >= 1 - rng.NextDouble())
                        Girl.AddSeenByWolf(wolf);
                }
            }
        }

        public class GirlVotePhase : SeperateVotingPhaseBase<GirlVote, Girl>
        {
            protected override GirlVote Create(Girl role, GameRoom game)
                => new GirlVote(role);

            protected override bool FilterVoter(Girl role)
            {
                return role.IsAlive;
            }

            protected override Girl GetRole(GirlVote voting)
                => voting.Girl;

            public override bool CanMessage(GameRoom game, Role role)
            {
                return role is WerwolfBase;
            }
        }

        public override void RemoveVoting(Voting voting)
        {
            base.RemoveVoting(voting);
            if (!Phase1.Votings.Any())
                foreach (var v in Phase2.Votings)
                    Phase2.RemoveVoting(v);
        }

        protected override void Init(GameRoom game)
        {
            base.Init(game);
            Phase1.GirlVotingPhase = Phase2;
        }

        public override bool CanExecute(GameRoom game)
        {
            return game.AliveRoles.Where(x => x is WerwolfBase).Any();
        }
    }
}
