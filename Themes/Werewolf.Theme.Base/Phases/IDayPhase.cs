namespace Werewolf.Theme.Phases;

public interface IDayPhase<T>
    where T : Scene, IDayPhase<T>
{
}
