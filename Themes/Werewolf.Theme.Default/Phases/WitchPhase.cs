using Werewolf.User;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Default.Roles;
using Werewolf.Theme.Votings;

namespace Werewolf.Theme.Default.Phases;

[Docs.Phase]
public class WitchPhase : MultiPhase<WitchPhase.WitchSafePhase, WitchPhase.WitchKillPhase>, INightPhase<WitchPhase>
{
    [Docs.Voting]
    public class WitchSafe : PlayerVotingBase
    {
        public Witch Witch { get; }

        public WitchSafe(Witch witch, GameRoom game, IEnumerable<UserId>? participants = null)
            : base(game, participants)
        {
            Witch = witch;
        }

        protected override bool DefaultParticipantSelector(Role role)
        {
            return role.KillInfo is KillInfos.KilledByWerwolf;
        }

        protected override bool AllowDoNothingOption => true;

        public override bool CanView(Role viewer)
        {
            return viewer == Witch;
        }

        public override bool CanVote(Role voter)
        {
            return voter == Witch;
        }

        public override void Execute(GameRoom game, UserId id, Role role)
        {
            role.RemoveKillFlag();
            Witch.UsedLivePotion = true;
        }
    }

    [Docs.Voting]
    public class WitchKill : PlayerVotingBase
    {
        public Witch Witch { get; }

        public WitchKill(Witch witch, GameRoom game, IEnumerable<UserId>? participants = null)
            : base(game, participants)
        {
            Witch = witch;
        }

        protected override bool DefaultParticipantSelector(Role role)
        {
            return role.IsAlive && !(role.KillInfo is KillInfos.KilledByWerwolf);
        }

        protected override bool AllowDoNothingOption => true;

        public override bool CanView(Role viewer)
        {
            return viewer is Witch;
        }

        public override bool CanVote(Role voter)
        {
            return voter == Witch && !Witch.UsedDeathPotion;
        }

        public override void Execute(GameRoom game, UserId id, Role role)
        {
            role.AddKillFlag(new KillInfos.KilledByWithDeathPotion());
            Witch.UsedDeathPotion = true;
        }
    }

    public class WitchSafePhase : SeperateVotingPhase<WitchSafe, Witch>
    {
        protected override WitchSafe Create(Witch role, GameRoom game, IEnumerable<UserId>? ids = null)
            => new WitchSafe(role, game, ids);

        protected override bool FilterVoter(Witch role)
            => role.IsAlive && !role.UsedLivePotion;

        protected override Witch GetRole(WitchSafe voting)
            => voting.Witch;

        public override void RemoveVoting(Voting voting)
        {
            base.RemoveVoting(voting);
        }

        public override bool CanMessage(GameRoom game, Role role)
        {
            return role is Witch;
        }
    }

    public class WitchKillPhase : SeperateVotingPhase<WitchKill, Witch>
    {
        protected override WitchKill Create(Witch role, GameRoom game, IEnumerable<UserId>? ids = null)
            => new WitchKill(role, game);

        protected override bool FilterVoter(Witch role)
            => role.IsAlive && !role.UsedDeathPotion;

        protected override Witch GetRole(WitchKill voting)
            => voting.Witch;

        public override bool CanMessage(GameRoom game, Role role)
        {
            return role is Witch;
        }
    }

    public override bool CanExecute(GameRoom game)
    {
        return base.CanExecute(game) &&
            !game.Users
                .Select(x => x.Value.Role)
                .Where(x => x is OldMan oldMan && oldMan.WasKilledByVillager)
                .Any();
    }
}
