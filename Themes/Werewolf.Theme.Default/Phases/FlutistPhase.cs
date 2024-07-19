using Werewolf.User;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Votings;
using Werewolf.Theme.Default.Roles;

namespace Werewolf.Theme.Default.Phases;

public class FlutistPhase : SeperateVotingPhase<FlutistPhase.FlutistPick, Flutist>, INightPhase<FlutistPhase>
{
    public class FlutistPick : PlayerVotingBase
    {
        public GameRoom Game { get; }

        public Flutist Flutist { get; }

        public FlutistPick(Flutist flutist, GameRoom game, IEnumerable<UserId>? participants = null)
            : base(game, participants ?? GetDefaultParticipants(game,
                role => role.Enabled
                    && role.Effects.GetEffect<Effects.FlutistEnchantEffect>(
                        x => x.Flutist == flutist
                    ) is null
                    && role is not Roles.Flutist
            ))
        {
            Game = game;
            Flutist = flutist;
        }

        public override bool CanView(Role viewer)
        {
            return viewer == Flutist;
        }

        protected override bool CanVoteBase(Role voter)
        {
            return voter == Flutist;
        }

        bool canFinish;

        protected override int GetMissingVotes(GameRoom game)
        {
            return canFinish ? 0 : 1;
        }

        public override void Execute(GameRoom game, UserId id, Role role)
        {
            role.Effects.Add(new Effects.FlutistEnchantEffect(Flutist));
            role.SendRoleInfoChanged();
        }

        public override string? Vote(GameRoom game, UserId voter, int id)
        {
            if (canFinish)
                return "cannot vote again";

            var option = Options
                .Where(x => x.id == id)
                .Select(x => x.option)
                .FirstOrDefault();
            
            if (option == null)
                return "option not found";

            if (option.Users.Any(x => x == voter))
                return null;
            
            option.Users.Add(voter);
            game.SendEvent(new Events.SetVotingVote(this, id, voter));

            var votes = GetResultUserIds().Count();
            var max = OptionsDict.Count < 8 ? 1 : 2;

            if (votes < max)
                return null;

            canFinish = true;
            CheckVotingFinished(game);
            return null;
        }
    }

    protected override void Init(GameRoom game)
    {
        base.Init(game);
    }

    public override bool CanMessage(GameRoom game, Role role)
    {
        return role is Roles.Flutist;
    }

    protected override FlutistPick Create(Flutist role, GameRoom game, IEnumerable<UserId>? ids = null)
    => new FlutistPick(role, game, ids);

    protected override bool FilterVoter(Flutist role)
    {
        return role.Enabled;
    }

    protected override Flutist GetRole(FlutistPick voting)
    => voting.Flutist;

    public override void ExecuteMultipleWinner(Voting voting, GameRoom game)
    {
        if (voting is FlutistPick pick)
        {
            foreach (var id in pick.GetResultUserIds())
            {
                var role = game.TryGetRole(id);
                if (role is null)
                    continue;
                pick.Execute(game, id, role);
            }
            RemoveVoting(pick);
        }
    }
}