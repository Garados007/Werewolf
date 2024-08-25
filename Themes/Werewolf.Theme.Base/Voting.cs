using System.Text.Json;
using Werewolf.Theme.Labels;
using Werewolf.User;

namespace Werewolf.Theme;

public abstract class Voting : ILabelHost<IVotingLabel>
{
    private static ulong nextId;
    public ulong Id { get; }

    public Voting(GameRoom game)
    {
        Id = unchecked(nextId++);
    }

    public LabelCollection<IVotingLabel> Labels { get; } = new();

    public abstract string LanguageId { get; }

    public bool Started { get; set; }

    private int finished;
    public bool Finished => finished > 0;

    public DateTime? Timeout { get; private set; }

    public abstract IEnumerable<(int id, VoteOption option)> Options { get; }

    public abstract bool CanView(GameRoom game, Character viewer);

    public bool CanVote(GameRoom game, Character voter)
    {
        return voter.Enabled && CanVoteBase(game, voter);
    }

    protected abstract bool CanVoteBase(GameRoom game, Character voter);

    protected virtual int GetMissingVotes(GameRoom game)
    {
        return game.Users
            .Where(x => x.Value.Character is not null && CanVote(game, x.Value.Character))
            .Where(x => !Options.Any(y => y.option.Users.Contains(x.Key)))
            .Count();
    }

    public bool SetTimeout(GameRoom game, bool force)
    {
        var count = GetMissingVotes(game);

        if (count <= 0)
        {
            FinishVoting(game);
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
                FinishVoting(game);
            }
        });
        return true;
    }

    public void Abort()
    {
        if (Interlocked.Exchange(ref finished, 1) > 0)
            return;
        Timeout = new DateTime();
    }

    public void FinishVoting(GameRoom game)
    {
        if (Interlocked.Exchange(ref finished, 1) > 0)
            return;
        var votes = GetResults().ToList();
        if (votes.Count == 1)
        {
            Execute(game, votes[0]);
        }
        else
        {
            Execute(game, votes.Count == 0 ? Options.Select(x => x.id) : votes);
        }
        _ = game.Votings.Remove(this);
        game.SendEvent(new Events.RemoveVoting(Id));
        AfterFinishExecute(game);
        game.Continue();
    }

    protected virtual void AfterFinishExecute(GameRoom game)
    {

    }

    public IEnumerable<Character> GetVoter(GameRoom game)
    {
        return game.AllCharacters.Where(x => CanVote(game, x));
    }

    private IEnumerable<int> GetResults()
    {
        var hasEntries = Options.Any();
        if (!hasEntries)
            return [];
        int max = Options.Max(x => x.option.Users.Count);
        return max == 0
            ? []
            : Options.Where(x => x.option.Users.Count == max)
            .Select(x => x.id);
    }

    protected abstract void Execute(GameRoom game, int id);

    protected abstract void Execute(GameRoom game, IEnumerable<int> ids);

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
            FinishVoting(game);
    }

    public static bool CanViewVoting(GameRoom game, UserInfo user, Character? ownRole, Voting voting)
    {
        return (game.Leader == user.Id && !game.LeaderIsPlayer) ||
            (ownRole != null && voting.CanView(game, ownRole));
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
        writer.WriteBoolean("can-vote", ownRole != null && CanVote(game, ownRole));
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
