using System.Collections.Concurrent;
using Werewolf.Theme.Labels;
using Werewolf.User;

namespace Werewolf.Theme;

public class GameRoom : ILabelHost<IGameRoomLabel>
{
    public int Id { get; }

    public uint ExecutionRound { get; private set; }

    public List<Event> Events { get; } = [];

    public List<Sequence> Sequences { get; } = [];

    public ThreadsafeCollection<Voting> Votings { get; } = [];

    public LabelCollection<IGameRoomLabel> Labels { get; } = new();

    private UserId leader;
    public UserId Leader
    {
        get => leader;
        set => SendEvent(new Events.OnLeaderChanged(leader = value));
    }

    public Phase? Phase { get; private set; }

    public bool HasActiveContent
        => Phase != null && (Votings.Count > 0 || Sequences.Count > 0);

    public ConcurrentDictionary<UserId, GameUserEntry> Users { get; }

    public ConcurrentDictionary<string, int> RoleConfiguration { get; }

    private bool leaderIsPlayer;
    public bool LeaderIsPlayer
    {
        get => leaderIsPlayer;
        set
        {
            if (leaderIsPlayer == value)
                return;
            if (!value && Users.TryGetValue(Leader, out GameUserEntry? leader))
                leader.Character = null;
            leaderIsPlayer = value;
        }
    }

    public bool DeadCanSeeAllRoles { get; set; }

    public bool AllCanSeeRoleOfDead { get; set; }

    public bool AutostartVotings { get; set; }

    public bool AutoFinishVotings { get; set; }

    public bool UseVotingTimeouts { get; set; }

    public bool AutoFinishRounds { get; set; }

    public GameMode? Theme { get; set; }

    public (uint round, ReadOnlyMemory<UserId> winner)? Winner { get; set; }

    public GameRoom(int id, UserInfo leader)
    {
        Id = id;
        Leader = leader.Id;
        Users = new ConcurrentDictionary<UserId, GameUserEntry>()
        {
            [leader.Id] = new GameUserEntry(leader),
        };
        RoleConfiguration = new ConcurrentDictionary<string, int>();
    }

    public bool AddParticipant(UserInfo user)
    {
        if (Leader == user.Id || Users.ContainsKey(user.Id))
            return false;

        _ = Users.TryAdd(user.Id, new GameUserEntry(user));
        SendEvent(new Events.AddParticipant(user));
        return true;
    }

    public bool RemoveParticipant(UserInfo user)
    {
        if (Users.Count > 1 && user.Id == Leader)
            return false;
        _ = Users.Remove(user.Id, out _);
        SendEvent(new Events.RemoveParticipant(user.Id));
        return true;
    }

    public IEnumerable<Character> AllCharacters
        => Users.Values
            .Where(x => x.Character is not null)
            .Select(x => x.Character!);

    /// <summary>
    /// Any existing roles that are consideres as alive. All close to death roles are excluded.
    /// </summary>
    public IEnumerable<Character> EnabledCharacters
        => AllCharacters.Where(x => x.Enabled);

    public Character? TryGetRole(UserId id)
    {
        return Users.TryGetValue(id, out GameUserEntry? entry) ? entry.Character : null;
    }

    public UserId? TryGetId(Character role)
    {
        foreach (var (id, entry) in Users)
            if (entry.Character == role)
                return id;
        return null;
    }

    public bool FullConfiguration => RoleConfiguration.Values.Sum() ==
        Users.Count + (LeaderIsPlayer ? 0 : -1);

    private int lockNextPhase;
    public void NextScene()
    {
        if (Interlocked.Exchange(ref lockNextPhase, 1) != 0)
            return;
        try
        {
            var cycleBreaker = new HashSet<Type>();
            while (Phase != null)
            {
            loadNextScene:
                if (Phase.NextScene(this))
                {
                    // check win condition
                    if (new WinCondition().Check(this, out ReadOnlyMemory<Character>? winner))
                    {
                        _ = StopGameAsync(winner);
                        return;
                    }
                    // start scene related stuff and finalize
                    if (!AutoFinishRounds || HasActiveContent)
                    {
                        Serilog.Log.Verbose("Core: Enter next scene");
                        SendEvent(new Events.NextPhase(Phase.CurrentScene));
                        foreach (var @event in Events)
                        {
                            AddSequence(@event.TargetPhase(Phase));
                            if (Phase.CurrentScene is not null)
                                AddSequence(@event.TargetScene(Phase.CurrentScene));
                        }
                        _ = Events.RemoveAll(x => x.Finished(this));
                        return;
                    }
                    // If there is no active content and the auto finish rounds is enabled, we can
                    // just skip to the next scene. This can be repeated indefinetely until we have
                    // an active scene or the phase has no more scenes to run.
                    Serilog.Log.Verbose("Core: Skip scene {name} without active content", Phase.CurrentScene?.LanguageId);
                    goto loadNextScene;
                }
                Phase = Phase.Next(this);
                Serilog.Log.Verbose("Core: Continue to phase {name}", Phase?.LanguageId);
                if (Phase is null || !cycleBreaker.Add(Phase.GetType()))
                {
                    Phase = null;
                    _ = StopGameAsync(null);
                    return;
                }
            }
        }
        finally
        {
            _ = Interlocked.Exchange(ref lockNextPhase, 0);
        }
    }

    public async Task StartGameAsync()
    {
        Winner = null;
        ExecutionRound++;
        Sequences.Clear();
        SendEvent(new Events.GameStart());
        // initialize all Character
        foreach (var character in EnabledCharacters)
            character.Init(this);
        // Setup phases
        Phase = Theme?.GetStartPhase(this);
        NextScene();
        // update user cache
        foreach (var (id, entry) in Users)
            entry.User =
                await Theme!.Users.GetUser(id, false)
                ?? throw new InvalidCastException();
        // post init
        Theme?.PostInit(this);

    }

    public async Task StopGameAsync(ReadOnlyMemory<Character>? winner)
    {
        ExecutionRound++;

        if (winner != null)
        {
            var winnerSpan = winner.Value;
            var winIds = new List<UserId>(winner.Value.Length);
            var xpMultiplier = AllCharacters.Count() * 0.15 - 0.15;
            try
            {
                await Task.WhenAll(Users.Select(
                    arg => UpdateScoreboard(arg.Key, arg.Value, winnerSpan, winIds, xpMultiplier)
                ));
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, "cannot calculate winner stats");
            }
            Winner = (ExecutionRound, winIds.ToArray());
        }
        Phase = null;
        SendEvent(new Events.GameEnd());
        if (winner != null)
            SendEvent(new Events.SendStats());
        foreach (var entry in AllCharacters)
            SendEvent(new Events.OnRoleInfoChanged(entry, ExecutionRound));
    }

    private Task UpdateScoreboard(UserId id, GameUserEntry entry, ReadOnlyMemory<Character> winnerSpan, List<UserId> winIds, double xpMultiplier)
    {
        uint dLeader = 0, dKilled = 0, dWinGames = 0, dLooseGames = 0;
        ulong dXP = 0;
        if (id == Leader && !LeaderIsPlayer)
        {
            dLeader++;
            dXP += (ulong)Math.Round(xpMultiplier * 100);
        }
        if (entry.Character != null)
        {
            if (entry.Character.Enabled)
            {
                dXP += (ulong)Math.Round(xpMultiplier * 160);
            }
            else
            {
                dKilled++;
            }

            bool won = false;
            foreach (var other in winnerSpan.Span)
                if (other == entry.Character)
                {
                    won = true;
                    break;
                }
            if (won)
            {
                dWinGames++;
                dXP += (ulong)Math.Round(xpMultiplier * 120);
                winIds.Add(id);
            }
            else
            {
                dLooseGames++;
            }
        }
        return entry.User.Stats.IncAsync(dWinGames, dKilled, dLooseGames, dLeader, dXP);
    }


    public event EventHandler<GameEvent>? OnEvent;

    public void SendEvent<T>(T @event)
        where T : GameEvent
    {
        OnEvent?.Invoke(this, @event);
        if (@event.GetLogMessage() is Chats.ChatServiceMessage message)
            SendChat(message);
    }

    public void SendChat<T>(T message)
        where T : Chats.ChatServiceMessage
        => SendEvent(message);

    public void Continue(bool force = false)
    {
        // check win condition
        if (new WinCondition().Check(this, out ReadOnlyMemory<Character>? winner))
        {
            _ = StopGameAsync(winner);
            return;
        }
        // if we still have a voting, never skip to the next
        if (Phase is null || Votings.Count > 0)
        {
            if (!force)
                return;
            ClearVotings();
        }
        // check if we can continue active sequences
        while (Sequences.Count > 0)
        {
            var last = Sequences[^1];
            last.Continue(this);
            if (!last.Active)
            {
                Sequences.RemoveAt(Sequences.Count - 1);
            }
            else
            {
                SendEvent(new Events.SendSequences());
                AutoContinueSequence();
                return;
            }
        }
        AutoContinueSequence();
        SendEvent(new Events.SendSequences());
        // continue to the next active scene
        if (AutoFinishRounds)
            NextScene();
    }

    private int lastSequenceCounter;
    private void AutoContinueSequence()
    {
        if (!AutoFinishRounds)
            return;
        var expectedCounter = Interlocked.Increment(ref lastSequenceCounter) + 1;
        if (Sequences.Count == 0)
            return;
        var gameStep = ExecutionRound;
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            var currentCounter = lastSequenceCounter;
            if (currentCounter != lastSequenceCounter || gameStep != ExecutionRound)
                return;
            Continue(false);
        });
    }

    public void AddSequence(Sequence? sequence)
    {
        if (sequence is null)
            return;
        SendEvent(new Events.SendSequences());
        Sequences.Add(sequence);
        AutoContinueSequence();
    }

    public void AddVoting(Voting voting)
    {
        Votings.Add(voting);
        voting.Started = AutostartVotings;
        if (UseVotingTimeouts)
            _ = voting.SetTimeout(this, true);
        SendEvent(new Events.AddVoting(voting));
    }

    public void RemoveVoting(Voting voting)
    {
        _ = Votings.Remove(voting);
        SendEvent(new Events.RemoveVoting(voting.Id));
    }

    public void ClearVotings()
    {
        foreach (var voting in Votings)
        {
            voting.Abort();
            SendEvent(new Events.RemoveVoting(voting.Id));
        }
        Votings.Clear();
    }

    public void AddEvent(Event @event)
    {
        AddSequence(@event.TargetNow);
        if (@event.Finished(this))
            return;
        Events.Add(@event);
    }

    public void AddRandomEvent()
    {
        if (Theme is null)
            return;
        var set = new HashSet<Type>();
        foreach (var type in Theme.GetEvents())
            _ = set.Add(type);
        while (set.Count > 0)
        {
            var type = set.ElementAt((int)Tools.GetRandom(set.Count));
            _ = set.Remove(type);
            var @event = Activator.CreateInstance(type) as Event;
            if (@event is not null && @event.Enable(this))
            {
                AddEvent(@event);
                return;
            }
        }
    }
}
