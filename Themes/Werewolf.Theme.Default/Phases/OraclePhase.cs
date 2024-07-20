using Werewolf.User;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Votings;
using Werewolf.Theme.Default.Roles;

namespace Werewolf.Theme.Default.Phases;

public class OraclePhase : SeperateVotingPhase<OraclePhase.OraclePick, Oracle>, INightPhase<OraclePhase>
{
    public class OraclePick : PlayerVotingBase
    {
        public Oracle Oracle { get; }

        public OraclePick(Oracle oracle, GameRoom game, IEnumerable<UserId>? participants = null)
            : base(game, participants ?? GetDefaultParticipants(
                game,
                role => role.Enabled
                    && role is not Roles.Oracle
                    && role.Effects.GetEffect<Effects.TrueIdentityShownEffect>(
                        x => x.Viewer == oracle
                    ) is null
            ))
        {
            Oracle = oracle;
        }

        public override bool CanView(Character viewer)
        {
            return viewer is Oracle;
        }

        protected override bool CanVoteBase(Character voter)
        {
            return voter == Oracle;
        }

        public override void Execute(GameRoom game, UserId id, Character role)
        {
            role.Effects.Add(new Effects.TrueIdentityShownEffect(Oracle));
            role.SendRoleInfoChanged();
        }
    }

    public override bool CanExecute(GameRoom game)
    {
        return !game.Users
            .Select(x => x.Value.Role)
            .Any(x => x is Roles.OldMan oldMan && oldMan.WasKilledByVillager)
            && base.CanExecute(game);
    }

    public override bool CanMessage(GameRoom game, Character role)
    {
        return role is Oracle;
    }

    protected override OraclePick Create(Oracle role, GameRoom game, IEnumerable<UserId>? ids = null)
    => new(role, game, ids);

    protected override bool FilterVoter(Oracle role)
    {
        return role.Enabled;
    }

    protected override Oracle GetRole(OraclePick voting)
    => voting.Oracle;
}
