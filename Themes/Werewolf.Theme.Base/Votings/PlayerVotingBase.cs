using Werewolf.Users.Api;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Votings
{
    public abstract class PlayerVotingBase : Voting
    {
        protected readonly ConcurrentDictionary<int, (UserId id, VoteOption opt)> options
            = new ConcurrentDictionary<int, (UserId id, VoteOption opt)>();

        public override IEnumerable<(int id, VoteOption option)> Options
            => options.Select(x => (x.Key, x.Value.opt));

        protected virtual bool AllowDoNothingOption { get; } = false;

        protected int? NoOptionId { get; }

        protected virtual string DoNothingOptionTextId { get; } = "do-nothing";

        protected virtual string PlayerTextId { get; } = "player";

        public PlayerVotingBase(GameRoom game, IEnumerable<UserId>? participants = null)
        {
            int index = 0;

            if (AllowDoNothingOption)
            {
                NoOptionId = index++;
                options.TryAdd(NoOptionId.Value, (new UserId(), new VoteOption(DoNothingOptionTextId)));
            }
            else NoOptionId = null;

            participants ??= game.Participants
                .Where(x => x.Value != null && DefaultParticipantSelector(x.Value))
                .Select(x => x.Key);

            foreach (var id in participants)
            {
                if (!game.UserCache.TryGetValue(id, out UserInfo? user))
                    user = null;
                options.TryAdd(index++, (id, new VoteOption(PlayerTextId, ("player", user?.Config.Username ?? $"User {id}"))));
            }
        }

        protected virtual bool DefaultParticipantSelector(Role role)
        {
            return role.IsAlive;
        }

        public IEnumerable<UserId> GetResultUserIds()
        {
            return GetResults()
                .Select(x => options.TryGetValue(x, out (UserId, VoteOption) r) ? (UserId?)r.Item1 : null)
                .Where(x => x != null)
                .Cast<UserId>();
        }

        public sealed override void Execute(GameRoom game, int id)
        {
            if (id != NoOptionId && options.TryGetValue(id, out (UserId user, VoteOption opt) result))
            {
                if (game.Participants.TryGetValue(result.user, out Role? role) && role != null)
                    Execute(game, result.user, role);
            }
        }

        public abstract void Execute(GameRoom game, UserId id, Role role);

        public virtual void RemoveOption(UserId user)
        {
            var key = options
                .Where(x => x.Value.id == user)
                .Select(x => (int?)x.Key)
                .FirstOrDefault();
            if (key != null)
                options.Remove(key.Value, out _);
        }
    }
}
