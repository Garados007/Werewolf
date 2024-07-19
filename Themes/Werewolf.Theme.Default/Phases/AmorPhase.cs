using Werewolf.User;
using Werewolf.Theme.Default.Roles;
using Werewolf.Theme.Default.Effects;
using Werewolf.Theme.Default.Effects.BeforeKillAction;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Votings;

namespace Werewolf.Theme.Default.Phases;

public class AmorPhase : SeperateVotingPhase<AmorPhase.AmorPick, Amor>, INightPhase<AmorPhase>
{
    public class AmorPick : PlayerVotingBase
    {
        public Amor Amor { get; }

        public AmorPick(Amor amor, GameRoom game, IEnumerable<UserId>? participants = null)
            : base(game, participants)
        {
            Amor = amor;
        }

        protected override bool AllowDoNothingOption => true;

        protected override string DoNothingOptionTextId => "do-love";

        private bool CanFinishVoting;

        public override bool CanView(Role viewer)
        {
            return viewer is Amor;
        }

        protected override bool CanVoteBase(Role voter)
        {
            return voter == Amor;
        }

        public override void Execute(GameRoom game, UserId id, Role role)
        {
            throw new InvalidOperationException("Narcissus mode not enabled.");
        }

        protected override int GetMissingVotes(GameRoom game)
        {
            return CanFinishVoting ? 0 : 1;
        }

        public override string? Vote(GameRoom game, UserId voter, int id)
        {
            if (CanFinishVoting)
                return "You already select finish";

            
            var votes = GetResultUserIds().Count();

            var option = Options
                .Where(x => x.id == id)
                .Select(x => x.option)
                .FirstOrDefault();

            if (option == null)
                return "option not found";

            if (option.Users.Any(x => x == voter))
            {
                var opt = option.Users.Where(x => x != voter).ToArray();
                option.Users.Clear();
                foreach (var item in opt)
                    option.Users.Add(item);
                game.SendEvent(new Events.RemoveVotingVote(this, id, voter));
            }
            else
            {
                if (votes == 2 && id != 0)
                    return "cannot select a third player";
                if (id == 0 && votes != 2)
                    return "you need to select two player first";
                option.Users.Add(voter);
                game.SendEvent(new Events.SetVotingVote(this, id, voter));
            }


            if (id == 0 && votes == 2)
            {
                CanFinishVoting = true;
            }

            CheckVotingFinished(game);

            return null;
        }
    }

    public override bool CanMessage(GameRoom game, Role role)
    => role is Amor;

    protected override AmorPick Create(Amor role, GameRoom game, IEnumerable<UserId>? ids = null)
    => new AmorPick(role, game, ids);

    protected override bool FilterVoter(Amor role)
    => role.Enabled;

    protected override Amor GetRole(AmorPick voting)
    => voting.Amor;

    public override void ExecuteMultipleWinner(Voting voting, GameRoom game)
    {
        if (voting is AmorPick pick)
        {
            var ids = pick.GetResultUserIds()
                .Select(x => game.TryGetRole(x))
                .Where(x => x is not null)
                .ToArray();
            for (int i = 0; i < ids.Length; ++i)
            {
                var source = ids[i]!;
                for (int j = 0; j < ids.Length; ++j)
                    if (i != j)
                    {
                        source.Effects.Add(new LovedEffect(pick.Amor, ids[j]!));
                    }
                if (source.Effects.GetEffect<KillByLove>() is null)
                    source.Effects.Add(new KillByLove());
                source.SendRoleInfoChanged();
            }
            RemoveVoting(voting);
        }
    }
}