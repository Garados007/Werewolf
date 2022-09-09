using Werewolf.User;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Votings;
using Werewolf.Theme.Default.Roles;

namespace Werewolf.Theme.Default.Phases
{
    public class DailyVictimElectionPhase : Phase, IDayPhase<DailyVictimElectionPhase>
    {
        public class DailyVote : PlayerVotingBase
        {
            internal readonly HashSet<Role>? allowedVoter;

            internal DailyVote(GameRoom game, IEnumerable<UserId>? participants, HashSet<Role>? allowedVoter)
                : this(game, participants)
            {
                this.allowedVoter = allowedVoter;
            }

            public DailyVote(GameRoom game, IEnumerable<UserId>? participants = null)
                : base(game, participants)
            {}

            protected override bool DefaultParticipantSelector(Role role)
            {
                return base.DefaultParticipantSelector(role) &&
                    (role is not Roles.Idiot idiot || !idiot.IsRevealed);
            }

            public override bool CanView(Role viewer)
            {
                return true;
            }

            protected override bool CanVoteBase(Role voter)
            {
                // special voting condition
                if (allowedVoter != null)
                    return allowedVoter.Contains(voter);
                // normal vote
                return voter.IsAlive && (voter is not Roles.Idiot idiot || !idiot.IsRevealed);
            }

            public override void Execute(GameRoom game, UserId id, Role role)
            {
                if (role is Roles.Idiot idiot)
                {
                    idiot.IsRevealed = true;
                    idiot.WasMajor = idiot.IsMajor;
                    idiot.IsMajor = false;
                    var oldManKilled = game.Users
                        .Select(x => x.Value.Role)
                        .Where(x => x is Roles.OldMan oldMan && !oldMan.IsAlive)
                        .Any();
                    if (oldManKilled)
                    {
                        idiot.IsRevealed = false;
                        idiot.AddKillFlag(new Effects.KillInfos.VillageKill());
                    }
                    return;
                }
                if (role is Roles.OldMan oldMan)
                {
                    oldMan.WasKilledByVillager = true;
                }
                role.AddKillFlag(new Effects.KillInfos.VillageKill());
            }

            protected override void AfterFinishExecute(GameRoom game)
            {
                base.AfterFinishExecute(game);
                if (allowedVoter != null)
                {
                    var scapegoat = game.Users
                        .Select(x => x.Value.Role)
                        .Where(x => x is Roles.ScapeGoat)
                        .Cast<Roles.ScapeGoat>()
                        .Where(x => x.TakingRevenge)
                        .FirstOrDefault();
                    if (scapegoat is not null)
                        scapegoat.TakingRevenge = false;
                }
            }
        }

        public class MajorPick : PlayerVotingBase
        {
            public MajorPick(GameRoom game, IEnumerable<UserId>? participants = null)
                : base(game, participants)
            {
            }

            public override bool CanView(Role viewer)
            {
                return true;
            }

            protected override bool CanVoteBase(Role voter)
            {
                return voter.IsMajor && voter.IsAlive;
            }

            public override void Execute(GameRoom game, UserId id, Role role)
            {
                role.AddKillFlag(new Effects.KillInfos.KilledByMajor());
            }
        }

        public override bool CanExecute(GameRoom game)
        {
            return true;
        }

        protected override void Init(GameRoom game)
        {
            base.Init(game);
            AddVoting(new DailyVote(game));
        }

        public override void ExecuteMultipleWinner(Voting voting, GameRoom game)
        {
            if (voting is DailyVote dv)
            {
                var hasMajor = game.AliveRoles.Any(x => x is BaseRole baserRole && x.IsMajor);
                var hasScapeGoat = game.AliveRoles.Any(x => x is Roles.ScapeGoat);
                var hasLostAbility = game.Users
                    .Select(x => x.Value.Role)
                    .Any(x => x is OldMan oldman && oldman.WasKilledByVillager);
                var ids = dv.GetResultUserIds().ToArray();
                if (ids.Length > 0)
                {
                    if (hasScapeGoat && !hasLostAbility)
                    {
                        foreach (var role in game.AliveRoles)
                            if (role is Roles.ScapeGoat scapeGoat)
                            {
                                // kill the scape goat and end the voting
                                scapeGoat.AddKillFlag(new Effects.KillInfos.ScapeGoatKilled());
                            }
                    }
                    else if (hasMajor)
                        AddVoting(new MajorPick(game, ids));
                    else AddVoting(new DailyVote(game, ids, dv.allowedVoter));
                }
                RemoveVoting(voting);
            }
            if (voting is MajorPick mp)
            {
                var ids = mp.GetResultUserIds().ToArray();
                if (ids.Length > 0)
                    AddVoting(new MajorPick(game, ids));
                RemoveVoting(voting);
            }
        }

        public override bool CanMessage(GameRoom game, Role role)
        {
            return true;
        }
    }
}
