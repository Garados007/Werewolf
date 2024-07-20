using System.Collections.Concurrent;
using Werewolf.User;

namespace Werewolf.Theme;

public class GameRoom
{
    public int Id { get; }

    public uint ExecutionRound { get; private set; }

    public Effects.EffectCollection<Effects.IGameRoomEffect> Effects { get; } = new();

    private UserId leader;
    public UserId Leader
    {
        get => leader;
        set => SendEvent(new Events.OnLeaderChanged(leader = value));
    }

    public PhaseFlow? Phase { get; private set; }

    public ConcurrentDictionary<UserId, GameUserEntry> Users { get; }

    public ConcurrentDictionary<Character, int> RoleConfiguration { get; }

    private bool leaderIsPlayer;
    public bool LeaderIsPlayer
    {
        get => leaderIsPlayer;
        set
        {
            if (leaderIsPlayer == value)
                return;
            if (!value && Users.TryGetValue(Leader, out GameUserEntry? leader))
                leader.Role = null;
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
        RoleConfiguration = new ConcurrentDictionary<Character, int>();
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

    /// <summary>
    /// Any existing roles that are consideres as alive. All close to death roles are excluded.
    /// </summary>
    public IEnumerable<Character> AliveRoles
        => Users.Values
            .Select(x => x.Role)
            .Where(x => x != null)
            .Cast<Character>()
            .Where(x => x.Enabled);

    public Character? TryGetRole(UserId id)
    {
        return Users.TryGetValue(id, out GameUserEntry? entry) ? entry.Role : null;
    }

    public UserId? TryGetId(Character role)
    {
        foreach (var (id, entry) in Users)
            if (entry.Role == role)
                return id;
        return null;
    }

    public bool FullConfiguration => RoleConfiguration.Values.Sum() ==
        Users.Count + (LeaderIsPlayer ? 0 : -1);

    private int lockNextPhase;
    public async Task NextPhaseAsync()
    {
        if (Interlocked.Exchange(ref lockNextPhase, 1) != 0)
            return;
        try
        {
            if (Phase != null && !await Phase.NextAsync(this))
                Phase = null;
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
        SendEvent(new Events.GameStart());
        // Setup phases
        Phase = Theme?.GetPhases(RoleConfiguration);
        if (Phase != null && (!Phase.Current.IsGamePhase || !Phase.Current.CanExecute(this)))
            await NextPhaseAsync();
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
            var xpMultiplier = Users.Values.Where(x => x.Role is not null).Count() * 0.15 - 0.15;
            try
            {
                await Task.WhenAll(Users.Select(
                    arg =>
                    {
                        var (id, entry) = arg;
                        uint dLeader = 0,
                            dKilled = 0,
                            dWinGames = 0,
                            dLooseGames = 0;
                        ulong dXP = 0;
                        if (id == Leader && !LeaderIsPlayer)
                        {
                            dLeader++;
                            dXP += (ulong)Math.Round(xpMultiplier * 100);
                        }
                        if (entry.Role != null)
                        {
                            if (entry.Role.Enabled)
                            {
                                dXP += (ulong)Math.Round(xpMultiplier * 160);
                            }
                            else
                            {
                                dKilled++;
                            }

                            bool won = false;
                            foreach (var other in winnerSpan.Span)
                                if (other == entry.Role)
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
        foreach (var entry in Users.Values)
            if (entry.Role != null)
                SendEvent(new Events.OnRoleInfoChanged(entry.Role, ExecutionRound));
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
}
