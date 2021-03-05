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

        public uint ExecutionRound { get; private set; } = 0;

        UserId leader = new UserId();
        public UserId Leader
        {
            get => leader;
            set
            {
                SendEvent(new Events.OnLeaderChanged(leader = value));
            }
        }

        public PhaseFlow? Phase { get; private set; }

        public ConcurrentDictionary<UserId, Role?> Participants { get; }

        public ConcurrentDictionary<UserId, UserInfo> UserCache { get; }

        public ConcurrentDictionary<Role, int> RoleConfiguration { get; }

        bool leaderIsPlayer = false;
        public bool LeaderIsPlayer
        {
            get => leaderIsPlayer;
            set
            {
                if (leaderIsPlayer == value)
                    return;
                if (value)
                    Participants.TryAdd(Leader, null);
                else Participants.TryRemove(Leader, out _);
                leaderIsPlayer = value;
            }
        }

        public bool DeadCanSeeAllRoles { get; set; } = false;

        public bool AllCanSeeRoleOfDead { get; set; } = false;

        public bool AutostartVotings { get; set; } = false;

        public bool AutoFinishVotings { get; set; } = false;

        public bool UseVotingTimeouts { get; set; } = false;

        public bool AutoFinishRounds { get; set; } = false;

        public Theme? Theme { get; set; }

        public (uint round, ReadOnlyMemory<UserId> winner)? Winner { get; set; }

        public GameRoom(int id, UserInfo leader)
        {
            Id = id;
            Leader = leader.Id;
            Participants = new ConcurrentDictionary<UserId, Role?>();
            UserCache = new ConcurrentDictionary<UserId, UserInfo>()
            {
                [leader.Id] = leader
            };
            RoleConfiguration = new ConcurrentDictionary<Role, int>();
        }

        public bool AddParticipant(UserInfo user)
        {
            if (Leader == user.Id || Participants.ContainsKey(user.Id))
                return false;

            Participants.TryAdd(user.Id, null);
            UserCache[user.Id] = user;
            SendEvent(new Events.AddParticipant(user));
            return true;
        }

        public bool RemoveParticipant(UserInfo user)
        {
            if (!Participants.IsEmpty && user.Id == Leader)
                return false;
            if (Participants!.Remove(user.Id, out _))
                UserCache.Remove(user.Id, out _);
            SendEvent(new Events.RemoveParticipant(user.Id));
            return true;
        }

        /// <summary>
        /// Any existing roles that are consideres as alive. All close to death roles are excluded.
        /// </summary>
        public IEnumerable<Role> AliveRoles
            => Participants.Values.Where(x => x != null).Cast<Role>().Where(x => x.IsAlive);

        /// <summary>
        /// Any existing roles that are not finally dead. Only roles that have the
        /// <see cref="KillState.Killed"/> are excluded.
        /// </summary>
        public IEnumerable<Role> NotKilledRoles
            => Participants.Values.Where(x => x != null).Cast<Role>().Where(x => x.KillState != KillState.Killed);

        public Role? TryGetRole(UserId id)
        {
            if (Participants.TryGetValue(id, out Role? role))
                return role;
            else return null;
        }

        public UserId? TryGetId(Role role)
        {
            foreach (var (id, prole) in Participants)
                if (prole == role)
                    return id;
            return null;
        }

        public bool FullConfiguration => RoleConfiguration.Values.Sum() == Participants.Count;

        int lockNextPhase = 0;
        public async Task NextPhaseAsync()
        {
            if (Interlocked.Exchange(ref lockNextPhase, 1) != 0)
                return;
            if (Phase != null && !await Phase.NextAsync(this))
                Phase = null;
            Interlocked.Exchange(ref lockNextPhase, 0);
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
            var users = UserCache.Keys.ToArray();
            foreach (var user in users)
                UserCache[user] = 
                    await Theme!.Users.GetUser(user, false) 
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
                // -0.15 is for the leader
                var xpMultiplier = Participants.Values.Where(x => x != null).Count() * 0.15;
                if (leaderIsPlayer) // we have one more player
                    xpMultiplier -= 0.15;
                await Task.WhenAll(UserCache.Select(
                    arg =>
                    {
                        var (id, user) = arg;
                        var change = new UserStats();
                        if (id == Leader && !LeaderIsPlayer)
                        {
                            change.Leader++;
                            change.CurrentXp += (ulong)Math.Round(xpMultiplier * 100);
                        }
                        if (Participants.TryGetValue(id, out Role? role) && role != null)
                        {
                            if (role.IsAlive)
                            {
                                change.CurrentXp += (ulong)Math.Round(xpMultiplier * 160);
                            }
                            else
                            {
                                change.Killed++;
                            }

                            bool won = false;
                            foreach (var other in winnerSpan.Span)
                                if (other == role)
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
            foreach (var role in Participants.Values)
                if (role != null)
                    SendEvent(new Events.OnRoleInfoChanged(role, ExecutionRound));
        }
    
        public event EventHandler<GameEvent>? OnEvent;

        public void SendEvent<T>(T @event)
            where T : GameEvent
        {
            OnEvent?.Invoke(this, @event);
        }
    }
}