namespace Werewolf.Theme.Labels;

public abstract class KillInfoEffect : ICharacterLabel
{
    public virtual string NotificationId
    {
        get
        {
            var name = GetType().FullName ?? "";
            var ind = name.LastIndexOf('.');
            return ind < 0 ? name : name[(ind + 1)..];
        }
    }

    public virtual IEnumerable<string> GetSeenTags(GameRoom game, Character current, Character? viewer)
    {
        yield break;
    }
}
