namespace Werewolf.Theme.Phases;

public abstract class DiscussionPhase : Phase
{
    /// <summary>
    /// The is a simple voting with only one option to end the voting. The player have time to
    /// discuss something if they need.
    /// </summary>
    [Docs.Voting]
    [Docs.Vote("end")]
    public class DiscussionEnd : Voting
    {
        private readonly VoteOption option;
        private readonly DiscussionPhase phase;

        public DiscussionEnd(DiscussionPhase phase)
        {
            option = new VoteOption("end");
            this.phase = phase;
        }

        public override IEnumerable<(int id, VoteOption option)> Options
            => Enumerable.Repeat((0, option), 1);

        public override bool CanView(Role viewer)
            => phase.CanView(viewer);

        public override bool CanVote(Role voter)
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
        AddVoting(new DiscussionEnd(this));
    }
}
