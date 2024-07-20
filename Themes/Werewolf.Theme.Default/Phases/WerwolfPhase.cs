using Werewolf.User;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Default.Roles;
using Werewolf.Theme.Votings;

namespace Werewolf.Theme.Default.Phases;

public class WerwolfPhase : MultiPhase<WerwolfPhase.WerwolfVotingPhase, WerwolfPhase.GirlVotePhase>, INightPhase<WerwolfPhase>
{
    public class WerwolfVote : PlayerVotingBase
    {
        public WerwolfVote(GameRoom game, IEnumerable<UserId>? participants = null)
            : base(game, participants ?? GetDefaultParticipants(game,
                role => role is not WerwolfBase && role.Enabled
            ))
        {
        }

        public override bool CanView(Character viewer)
        {
            return viewer is WerwolfBase;
        }

        protected override bool CanVoteBase(Character voter)
        {
            return voter is WerwolfBase && voter.Enabled;
        }

        public override void Execute(GameRoom game, UserId id, Character role)
        {
            role.AddKillFlag(new Effects.KillInfos.KilledByWerwolf());
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
            => new(game, ids);

        public override bool CanMessage(GameRoom game, Character role)
        {
            return role is WerwolfBase;
        }
    }

    public class GirlVote : Voting
    {
        private readonly List<VoteOption> options = new();
        public override IEnumerable<(int id, VoteOption option)> Options
            => options.Select((opt, id) => (id, opt));

        public Girl Girl { get; }

        public GirlVote(GameRoom game, Girl girl)
            : base(game)
        {
            Girl = girl;
            options.Add(new VoteOption("do-nothing"));
            options.Add(new VoteOption("spy"));
        }

        public override bool CanView(Character viewer)
            => viewer == Girl;

        protected override bool CanVoteBase(Character voter)
            => voter == Girl;

#if DEBUG
#pragma warning disable CS0649
        /// <summary>
        /// This seed is only used for the automatic test cases to have a deterministic
        /// behavior. In release mode this field is removed and always a pseudo random behavior
        /// is used. The test setup will use reflections to access this field.
        /// </summary>
        private static int? Seed;
#pragma warning restore CS0649
#endif

        public override void Execute(GameRoom game, int id)
        {
            if (id != 1)
                return;
#if DEBUG
            Console.WriteLine($"Use Seed {Seed}");
            var rng = Seed is null ? new Random() : new Random(Seed.Value);
#else
            var rng = new Random();
#endif
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
        public override bool CanExecute(GameRoom game)
        {
            return base.CanExecute(game) &&
                !game.Users.Select(x => x.Value.Role)
                    .Where(x => x is OldMan oldman && oldman.WasKilledByVillager)
                    .Any();
        }

        protected override GirlVote Create(Girl role, GameRoom game)
            => new(game, role);

        protected override bool FilterVoter(Girl role)
        {
            return role.Enabled;
        }

        protected override Girl GetRole(GirlVote voting)
            => voting.Girl;

        public override bool CanMessage(GameRoom game, Character role)
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
