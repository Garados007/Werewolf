using System;
using System.Collections.Generic;
using System.Linq;
using Test.Tools.User;
using Werewolf.Theme;
using Werewolf.User;

namespace Test.Tools;

public class Runner<TMode>
    where TMode : GameMode
{
    public TMode Mode { get; }

    public GameRoom GameRoom { get; }

    private readonly TestUserFactory factory;

    private readonly object lockCapturedEvents = new();
    private readonly List<GameEvent> capturedEvents = [];

    private static TMode Construct(GameRoom room, UserFactory factory)
    {
        var constructed = Activator.CreateInstance(typeof(TMode), room, factory);
        IsInstanceOfType<TMode>(
            constructed,
            $"{typeof(TMode).FullName} has no constructor that accepts {typeof(GameRoom).FullName} " +
            $"and {typeof(UserFactory)} as arguments"
        );
        return (TMode)constructed;
    }

    public Runner()
        : this(Construct)
    { }

    public Runner(Func<GameRoom, UserFactory, TMode> themeConstructor)
    {
        factory = new TestUserFactory();
        GameRoom = new GameRoom(0, factory.NewUser())
        {
            LeaderIsPlayer = false,
        };
        Mode = themeConstructor(GameRoom, factory);
        GameRoom.Theme = Mode;
        GameRoom.OnEvent += (_, e) =>
        {
            lock (lockCapturedEvents)
                capturedEvents.Add(e);
        };
    }

    public UserInfo AddUser()
    {
        var user = factory.NewUser();
        _ = GameRoom.AddParticipant(user);
        return user;
    }

    public bool RemoveUser(int index)
    {
        var (_, entry) = GameRoom.Users.ElementAt(index);
        return GameRoom.RemoveParticipant(entry.User);
    }

    public void InitUser(int newUserCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(newUserCount);
        if (GameRoom.Users.Count > newUserCount)
        {
            var users = GameRoom.Users
                .Select(x => x.Value.User)
                .ToArray();
            foreach (var user in users)
            {
                if (GameRoom.Users.Count > newUserCount)
                {
                    _ = GameRoom.RemoveParticipant(user);
                }
                else break;
            }
        }
        while (GameRoom.Users.Count < newUserCount)
        {
            _ = GameRoom.AddParticipant(factory.NewUser());
        }
    }

    public Runner<TMode> InitChars<TChar>(int count)
        where TChar : Character
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var name = Mode.GetCharacterName(typeof(TChar));
        IsNotNull(name, $"Character {typeof(TChar).Name} must be registered");
        foreach (var (id, entry) in GameRoom.Users)
        {
            if (id == GameRoom.Leader && !GameRoom.LeaderIsPlayer)
                continue;
            if (count == 0)
                break;
            if (entry.Character is not null)
                continue;
            entry.Character = Mode.CreateCharacter(name);
            count--;
        }
        for (int i = 0; i < count; ++i)
        {
            var user = factory.NewUser();
            _ = GameRoom.AddParticipant(user);
            GameRoom.Users[user.Id].Character = Mode.CreateCharacter(name);
        }
        _ = GameRoom.RoleConfiguration.AddOrUpdate(name, _ => count, (_, old) => old + count);
        return this;
    }

    public Runner<TMode> DefaultConfig()
    {
        GameRoom.LeaderIsPlayer = true;
        GameRoom.DeadCanSeeAllRoles = true;
        GameRoom.AutostartVotings = true;
        GameRoom.UseVotingTimeouts = true;
        GameRoom.AutoFinishRounds = true;
        return this;
    }

    public bool HasCapturedEvents()
        => capturedEvents.Count > 0;

    public bool HasCapturedEvents<TGameEvent>()
        where TGameEvent : GameEvent
        => capturedEvents.Any(x => x is TGameEvent);

    public bool HasCapturedEvents<TGameEvent>(Func<TGameEvent, bool> check)
        where TGameEvent : GameEvent
        => capturedEvents
            .Where(x => x is TGameEvent)
            .Cast<TGameEvent>()
            .Any(check);

    public void CollectEvent<TGameEvent>()
        where TGameEvent : GameEvent
        => CollectEvent<TGameEvent>(x => true);

    public void CollectEvent<TGameEvent>(Func<TGameEvent, bool> selector)
        where TGameEvent : GameEvent
    {
        for (int i = 0; i < capturedEvents.Count; ++i)
        {
            if (capturedEvents[i] is not TGameEvent @event || !selector(@event))
                continue;
            capturedEvents.RemoveAt(i);
            return;
        }
        throw new KeyNotFoundException($"Event {typeof(TGameEvent).FullName} is not captured");
    }

    public void ClearEvents()
        => capturedEvents.Clear();

    public void ClearEvents<TGameEvent>()
        where TGameEvent : GameEvent
        => ClearEvents<TGameEvent>(x => true);

    public void ClearEvents<TGameEvent>(Func<TGameEvent, bool> selector)
        where TGameEvent : GameEvent
    {
        _ = capturedEvents.RemoveAll(x => x is TGameEvent @event && selector(@event));
    }
}
