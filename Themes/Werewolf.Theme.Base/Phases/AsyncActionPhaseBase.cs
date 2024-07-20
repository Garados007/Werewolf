namespace Werewolf.Theme.Phases;

public abstract class AsyncActionPhaseBase : Scene
{
    public override bool CanExecute(GameRoom game)
        => true;

    public override bool IsGamePhase => false;

    public abstract Task ExecuteAsync(GameRoom game);

    public sealed override async Task InitAsync(GameRoom game)
    {
        await base.InitAsync(game);
        await ExecuteAsync(game);
    }

    protected sealed override void Init(GameRoom game)
    {
        base.Init(game);
    }

    public override bool CanMessage(GameRoom game, Role role)
    {
        return false;
    }
}
