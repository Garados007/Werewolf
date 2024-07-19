namespace Werewolf.Theme.Phases;

public abstract class DiscussionPhase : Phase
{
    public class DiscussionEnd : Voting
    {
        private readonly VoteOption option;
        private readonly DiscussionPhase phase;

        public DiscussionEnd(GameRoom game, DiscussionPhase phase)
            : base(game)
        {
            option = new VoteOption("end");
            this.phase = phase;
        }

        public override IEnumerable<(int id, VoteOption option)> Options
            => Enumerable.Repeat((0, option), 1);

        public override bool CanView(Role viewer)
            => phase.CanView(viewer);

        protected override bool CanVoteBase(Role voter)
            => phase.CanVote(voter);

        public override void Execute(GameRoom game, int id)
        {
        }
    }

    protected abstract bool CanView(Role viewer);

    protected abstract bool CanVote(Role voter);

    public override bool CanExecute(GameRoom game)
    {
        return game.Users.Values.Any(x => x.Role is not null && CanVote(x.Role));
    }

    protected override void Init(GameRoom game)
    {
        base.Init(game);
        AddVoting(new DiscussionEnd(game, this));
    }
}
