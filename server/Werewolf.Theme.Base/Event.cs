namespace Werewolf.Theme;

public abstract class Event
{
    public abstract bool Enable(GameRoom game);

    public abstract bool Finished(GameRoom game);

    public virtual Sequence? TargetNow { get; }

    public abstract Sequence? TargetPhase(Phase phase);

    public abstract Sequence? TargetScene(Scene scene);
}
