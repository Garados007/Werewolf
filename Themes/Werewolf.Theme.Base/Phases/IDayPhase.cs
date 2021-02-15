namespace Werewolf.Theme.Phases
{
    public interface IDayPhase<T>
        where T : Phase, IDayPhase<T>
    {
    }
}
