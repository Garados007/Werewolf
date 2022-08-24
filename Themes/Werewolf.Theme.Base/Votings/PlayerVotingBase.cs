using Werewolf.User;
using System.Collections.Concurrent;

namespace Werewolf.Theme.Votings;

[Docs.Vote("do-nothing", "The player decides to do nothing.")]
[Docs.Vote("player", "This option represents a player"),
    Docs.VoteVariable("player", "player", "The name of the player"),
    Docs.VoteVariable("player", "player-id", "The id of the player")
]
public abstract class PlayerVotingBase : Voting
{
    protected ConcurrentDictionary<int, (UserId id, VoteOption opt)> OptionsDict { get; }
        = new ConcurrentDictionary<int, (UserId id, VoteOption opt)>();

    public override IEnumerable<(int id, VoteOption option)> Options
        => OptionsDict.Select(x => (x.Key, x.Value.opt));

    public int? GetOptionIndex(UserId id)
    {
        return OptionsDict
            .Where(x => x.Value.id == id)
            .Select(x => (int?)x.Key)
            .FirstOrDefault();
    }

    protected virtual bool AllowDoNothingOption { get; }

    protected int? NoOptionId { get; }

    protected virtual string DoNothingOptionTextId { get; } = "do-nothing";

    protected virtual string PlayerTextId { get; } = "player";

    public PlayerVotingBase(GameRoom game, IEnumerable<UserId>? participants = null)
    {
        int index = 0;

        if (AllowDoNothingOption)
        {
            NoOptionId = index++;
            _ = OptionsDict.TryAdd(NoOptionId.Value, (new UserId(), new VoteOption(DoNothingOptionTextId)));
        }
        else NoOptionId = null;

        participants ??= game.Users
            .Where(x => x.Value.Role is not null && DefaultParticipantSelector(x.Value.Role))
            .Select(x => x.Key);

        foreach (var id in participants)
        {
            if (!game.Users.TryGetValue(id, out GameUserEntry? entry))
                entry = null;
            _ = OptionsDict.TryAdd(index++, 
                ( id
                , new VoteOption(PlayerTextId, 
                    ("player", entry?.User.Config.Username ?? $"User {id}"),
                    ("player-id", id.ToString())
                ))
            );
        }
    }

    protected virtual bool DefaultParticipantSelector(Role role)
    {
        return role.IsAlive;
    }

    public IEnumerable<UserId> GetResultUserIds()
    {
        return GetResults()
            .Select<int, UserId?>(x => OptionsDict.TryGetValue(x, out (UserId, VoteOption) r) ? r.Item1 : null)
            .Where(x => x is not null)
            .Select(x => x!.Value);
    }

    public sealed override void Execute(GameRoom game, int id)
    {
        if (id != NoOptionId && OptionsDict.TryGetValue(id, out (UserId user, VoteOption opt) result))
        {
            if (game.Users.TryGetValue(result.user, out GameUserEntry? entry) && 
                entry.Role is not null)
                Execute(game, result.user, entry.Role);
        }
    }

    public abstract void Execute(GameRoom game, UserId id, Role role);

    public virtual void RemoveOption(UserId user)
    {
        var key = OptionsDict
            .Where(x => x.Value.id == user)
            .Select(x => (int?)x.Key)
            .FirstOrDefault();
        if (key != null)
            _ = OptionsDict.Remove(key.Value, out _);
    }
}
