using System.Text.Json;
using Werewolf.User;

namespace Werewolf.Theme;

public abstract class Voting
{
    private static ulong nextId;
    public ulong Id { get; }

    public Voting(GameRoom game)
    {
        Id = unchecked(nextId++);

        // load the one time override of voter
        var ownType = GetType();
        var overrideEffect = game.Effects.GetEffect<Labels.OverrideVotingVoter>(
            x => ownType.IsAssignableTo(x.Voting)
        );
        if (overrideEffect is not null)
        {
            game.Effects.Remove(overrideEffect);
            overrideVoter = new HashSet<Character>();
            foreach (var id in overrideEffect)
            {
                var role = game.TryGetRole(id);
                if (role is not null)
                    overrideVoter.Add(role);
            }
        }
    }

    public Labels.LabelCollection<Labels.IVotingLabel> Effects { get; } = new();

    public virtual string LanguageId
    {
        get
        {
            var name = GetType().FullName ?? "";
            var ind = name.LastIndexOf('.');
            return ind >= 0 ? name[(ind + 1)..] : name;
        }
    }

    public bool Started { get; set; }

    private int finished;
    public bool Finished => finished > 0;

    public DateTime? Timeout { get; private set; }

    public abstract IEnumerable<(int id, VoteOption option)> Options { get; }

    public abstract bool CanView(Character viewer);

    private readonly HashSet<Character>? overrideVoter;
    public bool CanVote(Character voter)
    {
        return overrideVoter is not null ? overrideVoter.Contains(voter) : CanVoteBase(voter);
    }

    protected abstract bool CanVoteBase(Character voter);

    protected virtual int GetMissingVotes(GameRoom game)
    {
        return game.Users
            .Where(x => x.Value.Role is not null && CanVote(x.Value.Role))
            .Where(x => !Options.Any(y => y.option.Users.Contains(x.Key)))
            .Count();
    }

    public bool SetTimeout(GameRoom game, bool force)
    {
        var count = GetMissingVotes(game);

        if (count <= 0)
        {
            _ = FinishVotingAsync(game);
            return true;
        }

        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(45 * count);
        if (!force && Timeout != null && timeout - Timeout.Value < TimeSpan.FromSeconds(5))
            return false;

        Timeout = timeout;
        var gameStep = game.ExecutionRound;

        game.SendEvent(new Events.SetVotingTimeout(this));

        _ = Task.Run(async () =>
        {
            await Task.Delay(timeout - DateTime.UtcNow);
            if (game.ExecutionRound == gameStep && Timeout.Value == timeout)
            {
                // Timeout exceeded, can now skip
                await FinishVotingAsync(game);
            }
        });
        return true;
    }

    public async Task FinishVotingAsync(GameRoom game)
    {
        if (Interlocked.Exchange(ref finished, 1) > 0)
            return;
        var vote = GetResult();
        if (vote != null)
        {
            Execute(game, vote.Value);
            game.Phase?.Current.RemoveVoting(this);
        }
        else
        {
            game.Phase?.Current.ExecuteMultipleWinner(this, game);
        }
        AfterFinishExecute(game);
        if (new WinCondition().Check(game, out ReadOnlyMemory<Character>? winner))
        {
            await game.StopGameAsync(winner);
        }
        if (game.AutoFinishRounds && (!game.Phase?.Current.Votings.Any() ?? false))
        {
            await game.NextPhaseAsync();
        }
    }

    protected virtual void AfterFinishExecute(GameRoom game)
    {

    }

    public IEnumerable<Character> GetVoter(GameRoom game)
    {
        foreach (var role in game.Users.Select(x => x.Value.Role))
            if (role != null && CanVote(role))
                yield return role;
    }

    public virtual int? GetResult()
    {
        var options = GetResults().ToArray();
        return options.Length == 1 ? options[0] : (int?)null;
    }

    public virtual IEnumerable<int> GetResults()
    {
        var hasEntries = Options.Any();
        if (!hasEntries)
            return Options.Select(x => x.id);
        int max = Options.Max(x => x.option.Users.Count);
        return max == 0
            ? Enumerable.Empty<int>()
            : Options.Where(x => x.option.Users.Count == max)
            .Select(x => x.id);
    }

    public abstract void Execute(GameRoom game, int id);

    public virtual string? Vote(GameRoom game, UserId voter, int id)
    {
        if (Options.Any(x => x.option.Users.Contains(voter)))
            return "already voted";

        var option = Options
            .Where(x => x.id == id)
            .Select(x => x.option)
            .FirstOrDefault();

        if (option == null)
            return "option not found";

        string? error;
        if ((error = Vote(game, voter, option)) != null)
            return error;

        game.SendEvent(new Events.SetVotingVote(this, id, voter));

        CheckVotingFinished(game);

        return null;
    }

    protected virtual string? Vote(GameRoom game, UserId voter, VoteOption option)
    {
        option.Users.Add(voter);
        return null;
    }

    public virtual void CheckVotingFinished(GameRoom game)
    {
        if (game.UseVotingTimeouts)
            _ = SetTimeout(game, true);

        if (game.AutoFinishVotings && GetMissingVotes(game) == 0)
            _ = FinishVotingAsync(game);
    }

    public static bool CanViewVoting(GameRoom game, UserInfo user, Character? ownRole, Voting voting)
    {
        return (game.Leader == user.Id && !game.LeaderIsPlayer) ||
            (ownRole != null && voting.CanView(ownRole));
    }

    private static readonly System.Globalization.NumberFormatInfo format =
        System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

    public void WriteToJson(Utf8JsonWriter writer, GameRoom game, UserInfo user)
    {
        var ownRole = game.TryGetRole(user.Id);
        writer.WriteStartObject(); // {}
        writer.WriteString("id", Id.ToString(format));
        writer.WriteString("lang-id", LanguageId);
        writer.WriteBoolean("started", Started);
        writer.WriteBoolean("can-vote", ownRole != null && CanVote(ownRole));
        writer.WriteNumber("max-voter", GetVoter(game).Count());
        if (Timeout != null)
            writer.WriteString("timeout", Timeout.Value);
        else writer.WriteNull("timeout");
        writer.WriteStartObject("options"); // optionsDict
        foreach (var (id, option) in Options)
        {
            writer.WriteStartObject(id.ToString(format)); // id
            writer.WriteString("lang-id", option.LangId);
            writer.WriteStartObject("vars");
            foreach (var (key, value) in option.Vars)
                writer.WriteString(key, value);
            writer.WriteEndObject();
            writer.WriteStartArray("user");
            foreach (var vuser in option.Users)
                writer.WriteStringValue(vuser);
            writer.WriteEndArray();
            writer.WriteEndObject(); // id
        }
        writer.WriteEndObject(); // optionsDict
        writer.WriteEndObject(); // {}
    }
}
