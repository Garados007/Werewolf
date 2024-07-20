using Werewolf.User;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Default.Roles;
using Werewolf.Theme.Votings;

namespace Werewolf.Theme.Default.Phases;

public class HunterPhase : SeperateVotingPhase<HunterPhase.HunterKill, Hunter>
{
    public class HunterKill : PlayerVotingBase
    {
        public Hunter Hunter { get; }

        public HunterKill(GameRoom game, Hunter hunter, IEnumerable<UserId>? participants = null)
            : base(game, participants)
        {
            Hunter = hunter;
        }

        public override bool CanView(Character viewer)
        {
            return viewer == Hunter;
        }

        protected override bool CanVoteBase(Character voter)
        {
            return voter == Hunter;
        }

        public override void Execute(GameRoom game, UserId id, Character role)
        {
            role.AddKillFlag(new Effects.KillInfos.KilledByHunter());
            Hunter.HasKilled = true;
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

    protected override HunterKill Create(Hunter role, GameRoom game, IEnumerable<UserId>? ids = null)
        => new(game, role, ids);

    protected override Hunter GetRole(HunterKill voting)
        => voting.Hunter;

    protected override bool FilterVoter(Hunter role)
        => role.Enabled && role.HasKillFlag && !role.HasKilled;

    public override bool CanMessage(GameRoom game, Character role)
    {
        return true;
    }
}
