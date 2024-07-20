namespace Werewolf.Theme;

public abstract class Scene
{

    public Effects.EffectCollection<Effects.IPhaseEffect> Effects { get; } = new();

    public virtual string LanguageId
    {
        get
        {
            var name = GetType().FullName ?? "";
            var ind = name.LastIndexOf('.');
            return ind >= 0 ? name[(ind + 1)..] : name;
        }
    }

    public abstract bool CanExecute(GameRoom game);

    public abstract bool CanMessage(GameRoom game, Role role);

    public virtual async Task InitAsync(GameRoom game)
    {
        Init(game);
        await Task.CompletedTask;
    }

    protected virtual void Init(GameRoom game)
    {
        votings.Clear();
        this.game = game;
    }

    public virtual void Exit(GameRoom game) { }

    public virtual bool IsGamePhase => true;

    private readonly List<Voting> votings = new List<Voting>();
    public virtual IEnumerable<Voting> Votings => votings.ToArray();

    private GameRoom? game;

    protected virtual void AddVoting(Voting voting)
    {
        if (!voting.Options.Any())
            return;
        votings.Add(voting);
        voting.Started = game?.AutostartVotings ?? false;
        if (game?.UseVotingTimeouts ?? false)
            _ = voting.SetTimeout(game, true);
        game?.SendEvent(new Events.AddVoting(voting));
    }

    public virtual void RemoveVoting(Voting voting)
    {
        _ = votings.Remove(voting);
        game?.SendEvent(new Events.RemoveVoting(voting.Id));
    }

    public virtual void ExecuteMultipleWinner(Voting voting, GameRoom game)
    {

    }
}
