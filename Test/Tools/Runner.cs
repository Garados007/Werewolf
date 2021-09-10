using System;
using System.Collections.Generic;
using System.Linq;
using Test.Tools.User;
using Werewolf.Theme;
using Werewolf.User;

namespace Test.Tools
{
    public class Runner<T>
        where T : Theme
    {
        public T Theme { get; }

        public GameRoom GameRoom { get; }

        private readonly TestUserFactory factory;

        private readonly object lockCapturedEvents = new object();
        private readonly List<GameEvent> capturedEvents = new List<GameEvent>();

        private static T Construct(GameRoom room, UserFactory factory)
        {
            var constructed = Activator.CreateInstance(typeof(T), room, factory);
            if (constructed is T theme)
                return theme;
            else throw new InvalidOperationException(
                $"{typeof(T).FullName} has no constructor that accepts {typeof(GameRoom).FullName} " +
                $"and {typeof(UserFactory)} as arguments"
            );
        }

        public Runner()
            : this(Construct)
        {}

        public Runner(Func<GameRoom, UserFactory, T> themeConstructor)
        {
            factory = new TestUserFactory();
            GameRoom = new GameRoom(0, factory.NewUser())
            {
                LeaderIsPlayer = false,
            };
            Theme = themeConstructor(GameRoom, factory);
            GameRoom.Theme = Theme;
            GameRoom.OnEvent += (_, e) =>
            {
                lock (lockCapturedEvents)
                    capturedEvents.Add(e);
            };
        }

        public UserInfo AddUser()
        {
            var user = factory.NewUser();
            GameRoom.AddParticipant(user);
            return user;
        }

        public bool RemoveUser(int index)
        {
            var (_, entry) = GameRoom.Users.ElementAt(index);
            return GameRoom.RemoveParticipant(entry.User);
        }

        public void InitUser(int newUserCount)
        {
            if (newUserCount < 0)
                throw new ArgumentOutOfRangeException(nameof(newUserCount));
            if (GameRoom.Users.Count > newUserCount)
            {
                var users = GameRoom.Users
                    .Select(x => x.Value.User)
                    .ToArray();
                foreach (var user in users)
                {
                    if (GameRoom.Users.Count > newUserCount)
                    {
                        GameRoom.RemoveParticipant(user);
                    }
                    else break;
                }
            }
            while (GameRoom.Users.Count < newUserCount)
            {
                GameRoom.AddParticipant(factory.NewUser());
            }
        }
    
        public Runner<T> InitRoles<TRole>(TRole role, int count)
            where TRole : Role
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            foreach (var (id, entry) in GameRoom.Users)
            {
                if (id == GameRoom.Leader && !GameRoom.LeaderIsPlayer)
                    continue;
                if (count == 0)
                    break;
                if (entry.Role is not null)
                    continue;
                entry.Role = role.CreateNew();
                count--;
            }
            for (int i = 0; i < count; ++i)
            {
                var user = factory.NewUser();
                GameRoom.AddParticipant(user);
                GameRoom.Users[user.Id].Role = role.CreateNew();
            }
            Role? key = GameRoom.RoleConfiguration.Keys
                .Where(x => x is TRole)
                .FirstOrDefault();
            if (key is null)
                GameRoom.RoleConfiguration.TryAdd(role, count);
            else GameRoom.RoleConfiguration.AddOrUpdate(key, _ => count, (_, old) => old + count);
            return this;
        }

        public Runner<T> InitRoles<TRole>(int count)
            where TRole : Role
        {
            var prototype = (TRole?)Activator.CreateInstance(
                typeof(TRole),
                Theme
            );
            if (prototype is null)
                throw new ArgumentException($"Type {typeof(TRole).FullName} has no constructor that expects one param of type {typeof(Theme).FullName}");
            return InitRoles(prototype, count);
        }

        public bool HasCapturedEvents()
            => capturedEvents.Count > 0;
        
        public bool HasCapturedEvents<E>()
            where E : GameEvent
            => capturedEvents.Where(x => x is E).Any();
        
        public bool HasCapturedEvents<E>(Func<E, bool> check)
            where E : GameEvent
            => capturedEvents
                .Where(x => x is E)
                .Cast<E>()
                .Where(check)
                .Any();

        public void CollectEvent<E>()
            where E : GameEvent
            => CollectEvent<E>(x => true);
        
        public void CollectEvent<E>(Func<E, bool> selector)
            where E : GameEvent
        {
            for (int i = 0; i < capturedEvents.Count; ++i)
            {
                if (capturedEvents[i] is not E @event || !selector(@event))
                    continue;
                capturedEvents.RemoveAt(i);
                return;
            }
            throw new KeyNotFoundException($"Event {typeof(E).FullName} is not captured");
        }
    
        public void ClearEvents()
            => capturedEvents.Clear();

        public void ClearEvents<E>()
            where E : GameEvent
            => ClearEvents<E>(x => true);

        public void ClearEvents<E>(Func<E, bool> selector)
            where E : GameEvent
        {
            capturedEvents.RemoveAll(x => x is E @event && selector(@event));
        }
    }
}
