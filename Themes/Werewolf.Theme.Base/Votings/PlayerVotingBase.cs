using Werewolf.User;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Votings
{
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
            : base(game)
        {
            // check if someone has overwritten the participant selection for this one time

            var ownType = GetType();
            var overrideEffect = game.Effects.GetEffect<Effects.OverrideVotingParticipants>(
                x => ownType.IsAssignableTo(x.Voting)
            );
            if (overrideEffect is not null)
            {
                participants = overrideEffect;
                game.Effects.Remove(overrideEffect);
            }

            // init do nothing

            int index = 0;

            if (AllowDoNothingOption)
            {
                NoOptionId = index++;
                _ = OptionsDict.TryAdd(NoOptionId.Value, (new UserId(), new VoteOption(DoNothingOptionTextId)));
            }
            else NoOptionId = null;

            // load participants if not overwritten

            participants ??= game.Users
                .Where(x => x.Value.Role is not null && DefaultParticipantSelector(x.Value.Role))
                .Select(x => x.Key);
            
            // create option for participants

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
                .Where(x => x != NoOptionId)
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
                {
                    Execute(game, result.user, entry.Role);
                    return;
                }
            }
            // it could be that no option won this voting, but this has to be handled with the
            // multiple winner method.
            game.Phase?.Current.ExecuteMultipleWinner(this, game);
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
}
