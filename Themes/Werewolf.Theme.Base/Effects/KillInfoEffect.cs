namespace Werewolf.Theme.Effects;

public abstract class KillInfoEffect : IRoleEffect
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
