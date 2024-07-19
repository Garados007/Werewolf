using System.Collections.Concurrent;
using Werewolf.User;

namespace Werewolf.Theme;

public class VoteOption
{
    public string LangId { get; }

    public Dictionary<string, string> Vars { get; }

    public ConcurrentBag<UserId> Users { get; }
        = new ConcurrentBag<UserId>();

    public VoteOption(string langId, params (string key, string value)[] vars)
    {
        LangId = langId;
        Vars = new Dictionary<string, string>();
        foreach (var (key, value) in vars)
            Vars.Add(key, value);
    }

    public VoteOption(string langId, Dictionary<string, string> vars)
    {
        LangId = langId;
        Vars = vars;
    }
}
