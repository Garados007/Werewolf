using Werewolf.User;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Default.Roles;
using Werewolf.Theme.Votings;
using Werewolf.Theme.Default.Effects.KillInfos;

namespace Werewolf.Theme.Default.Phases;

public class WitchPhase : MultiPhase<WitchPhase.WitchSafePhase, WitchPhase.WitchKillPhase>, INightPhase<WitchPhase>
{
    public class WitchSafe : PlayerVotingBase
    {
        public Witch Witch { get; }

        public WitchSafe(Witch witch, GameRoom game, IEnumerable<UserId>? participants = null)
            : base(game, participants ?? GetDefaultParticipants(game,
                role => role.Effects.GetEffect<KilledByWerwolf>() is not null
            ))
        {
            Witch = witch;
        }

        protected override bool AllowDoNothingOption => true;

        public override bool CanView(Role viewer)
        {
            return viewer == Witch;
        }

        protected override bool CanVoteBase(Role voter)
        {
            return voter == Witch;
        }

        public override void Execute(GameRoom game, UserId id, Role role)
        {
            role.RemoveKillFlag();
            Witch.UsedLivePotion = true;
        }
    }

    public class WitchKill : PlayerVotingBase
    {
        public Witch Witch { get; }

        public WitchKill(Witch witch, GameRoom game, IEnumerable<UserId>? participants = null)
            : base(game, participants ?? GetDefaultParticipants(game,
                role => role.Enabled && role.Effects.GetEffect<KilledByWerwolf>() is null
            ))
        {
            Witch = witch;
        }

        protected override bool AllowDoNothingOption => true;

        public override bool CanView(Role viewer)
        {
            return viewer is Witch;
        }

        protected override bool CanVoteBase(Role voter)
        {
            return voter == Witch && !Witch.UsedDeathPotion;
        }

        public override void Execute(GameRoom game, UserId id, Role role)
        {
            role.AddKillFlag(new KilledByWithDeathPotion());
            Witch.UsedDeathPotion = true;
        }
    }

    public class WitchSafePhase : SeperateVotingPhase<WitchSafe, Witch>
    {
        protected override WitchSafe Create(Witch role, GameRoom game, IEnumerable<UserId>? ids = null)
            => new(role, game, ids);

        protected override bool FilterVoter(Witch role)
            => role.Enabled && !role.UsedLivePotion;

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
            => new(role, game);

        protected override bool FilterVoter(Witch role)
            => role.Enabled && !role.UsedDeathPotion;

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
