namespace Werewolf.Theme;

public abstract class Sequence
{
    public int Step { get; private set; }

    public abstract int MaxStep { get; }

    public bool Active { get; private set; } = true;

    public abstract void Continue(GameRoom game);

    public abstract string Name { get; }

    public string? StepName => GetName(Step - 1);

    protected abstract string? GetName(int step);

    protected void Next()
    {
        Step++;
    }

    protected void Stop()
    {
        Active = false;
    }

    public virtual void WriteMeta(System.Text.Json.Utf8JsonWriter writer, GameRoom game, User.UserInfo target)
    {

    }
}
