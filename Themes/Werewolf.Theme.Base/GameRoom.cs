using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werewolf.Users.Api;

namespace Werewolf.Theme
{
    public class GameRoom
    {
        public int Id { get; }

        public uint ExecutionRound { get; private set; }

        private UserId leader = new UserId();
        public UserId Leader
        {
            get => leader;
            set => SendEvent(new Events.OnLeaderChanged(leader = value));
        }

        public PhaseFlow? Phase { get; private set; }

        public ConcurrentDictionary<UserId, GameUserEntry> Users { get; }

        public ConcurrentDictionary<Role, int> RoleConfiguration { get; }

        private bool leaderIsPlayer;
        public bool LeaderIsPlayer
        {
            get => leaderIsPlayer;
            set
            {
                if (leaderIsPlayer == value)
                    return;
                if (!value &&  Users.TryGetValue(Leader, out GameUserEntry? leader))
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

        public Theme? Theme { get; set; }

        public (uint round, ReadOnlyMemory<UserId> winner)? Winner { get; set; }

        public GameRoom(int id, UserInfo leader)
        {
            Id = id;
            Leader = leader.Id;
            Users = new ConcurrentDictionary<UserId, GameUserEntry>()
            {
                [leader.Id] = new GameUserEntry(leader),
            };
            RoleConfiguration = new ConcurrentDictionary<Role, int>();
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
            if (!Users.IsEmpty && user.Id == Leader)
                return false;
            _ = Users.Remove(user.Id, out _);
            SendEvent(new Events.RemoveParticipant(user.Id));
            return true;
        }

        /// <summary>
        /// Any existing roles that are consideres as alive. All close to death roles are excluded.
        /// </summary>
        public IEnumerable<Role> AliveRoles
            => Users.Values
                .Select(x => x.Role)
                .Where(x => x != null)
                .Cast<Role>()
                .Where(x => x.IsAlive);

        /// <summary>
        /// Any existing roles that are not finally dead. Only roles that have the
        /// <see cref="KillState.Killed"/> are excluded.
        /// </summary>
        public IEnumerable<Role> NotKilledRoles
            => Users.Values
                .Select(x => x.Role)
                .Where(x => x != null)
                .Cast<Role>()
                .Where(x => x.KillState != KillState.Killed);

        public Role? TryGetRole(UserId id)
        {
            return Users.TryGetValue(id, out GameUserEntry? entry) ? entry.Role : null;
        }

        public UserId? TryGetId(Role role)
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
            if (Phase != null && !await Phase.NextAsync(this))
                Phase = null;
            _ = Interlocked.Exchange(ref lockNextPhase, 0);
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

        public async Task StopGameAsync(ReadOnlyMemory<Role>? winner)
        {
            ExecutionRound++;

            if (winner != null)
            {
                var winnerSpan = winner.Value;
                var winIds = new List<UserId>(winner.Value.Length);
                var xpMultiplier = Users.Values.Where(x => x.Role is not null).Count() * 0.15 - 0.15;
                await Task.WhenAll(Users.Select(
                    arg =>
                    {
                        var (id, entry) = arg;
                        var change = new UserStats();
                        if (id == Leader && !LeaderIsPlayer)
                        {
                            change.Leader++;
                            change.CurrentXp += (ulong)Math.Round(xpMultiplier * 100);
                        }
                        if (entry.Role != null)
                        {
                            if (entry.Role.IsAlive)
                            {
                                change.CurrentXp += (ulong)Math.Round(xpMultiplier * 160);
                            }
                            else
                            {
                                change.Killed++;
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
                                change.WinGames++;
                                change.CurrentXp += (ulong)Math.Round(xpMultiplier * 120);
                                winIds.Add(id);
                            }
                            else
                            {
                                change.LooseGames++;
                            }
                        }
                        return Theme!.Users.UpdateUserStats(id, change);
                    }
                ));
                Winner = (ExecutionRound, winIds.ToArray());
            }
            Phase = null;
            SendEvent(new Events.GameEnd());
            foreach (var entry in Users.Values)
                if (entry.Role != null)
                    SendEvent(new Events.OnRoleInfoChanged(entry.Role, ExecutionRound));
        }

        public event EventHandler<GameEvent>? OnEvent;

        public void SendEvent<T>(T @event)
            where T : GameEvent
        {
            OnEvent?.Invoke(this, @event);
        }
    }
}