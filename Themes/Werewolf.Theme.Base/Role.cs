using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.User;
using Werewolf.Theme.Effects;

namespace Werewolf.Theme
{
    /// <summary>
    /// The basic role every game user has. Any special state is encoded in this role.
    /// </summary>
    public abstract class Role
    {
        public EffectCollection<IRoleEffect> Effects { get; } = new();

        private bool isAlive = true;
        public bool IsAlive
        {
            get => isAlive;
            private set
            {
                isAlive = value;
                SendRoleInfoChanged();
            }
        }

        public bool HasKillFlag => Effects.GetEffect<KillInfoEffect>() is not null;

        private bool isMajor;
        public bool IsMajor
        {
            get => isMajor;
            set
            {
                isMajor = value;
                SendRoleInfoChanged();
            }
        }

        /// <summary>
        /// Get a list of special tags that are defined for this role. 
        /// </summary>
        /// <param name="game">The current game</param>
        /// <param name="viewer">The viewer of this role. null for the leader</param>
        /// <returns>a list of defined tags</returns>
        public virtual IEnumerable<string> GetTags(GameRoom game, Role? viewer)
        {
            if (!IsAlive)
                yield return "not-alive";
            if (IsMajor)
                yield return "major";
            foreach (var effect in Effects.GetEffects())
                foreach (var tag in effect.GetSeenTags(game, this, viewer))
                    yield return tag;
        }

        public void SendRoleInfoChanged()
        {
            Theme.Game?.SendEvent(new Events.OnRoleInfoChanged(this));
        }

        public Theme Theme { get; }

        public Role(Theme theme)
        {
            Theme = theme ?? throw new ArgumentNullException(nameof(theme));
        }

        public abstract bool? IsSameFaction(Role other);

        public virtual Role ViewRole(Role viewer)
        {
            return Theme.GetBasicRole();
        }

        public abstract Role CreateNew();

        public abstract string Name { get; }

        /// <summary>
        /// Add the kill effect to the list. Mark this as to be killed somewhere in the future.
        /// </summary>
        /// <param name="info">the kill info</param>
        public void AddKillFlag(KillInfoEffect info)
        {
            if (!IsAlive)
                return;
            Effects.Add(info);
        }

        /// <summary>
        /// Remove any kill effects. This role wont be killed so far.
        /// </summary>
        public void RemoveKillFlag()
        {
            Effects.Remove<KillInfoEffect>();
        }

        /// <summary>
        /// Mark this role as killed. This will also remove any kill flags. If no kill flags were
        /// attached nothing will happen.
        /// </summary>
        public void ChangeToKilled()
        {
            if (Effects.Remove<KillInfoEffect>() > 0)
                IsAlive = false;
        }

        public static Role? GetSeenRole(GameRoom game, uint? round, UserInfo user, UserId targetId, Role target)
        {
            var ownRole = game.TryGetRole(user.Id);
            return (game.Leader == user.Id && !game.LeaderIsPlayer) ||
                    targetId == user.Id ||
                    round == game.ExecutionRound ||
                    (game.AllCanSeeRoleOfDead && !target.IsAlive) ||
                    (ownRole != null && game.DeadCanSeeAllRoles && !ownRole.IsAlive) ?
                target :
                ownRole != null ?
                target.ViewRole(ownRole) :
                null;
        }

        public static IEnumerable<string> GetSeenTags(GameRoom game, UserInfo user, Role? viewer, Role target)
        {
            if (viewer == null && game.Leader != user.Id)
                return Enumerable.Empty<string>();
            if (viewer != null && game.DeadCanSeeAllRoles && !viewer.IsAlive)
                viewer = null;
            return target.GetTags(game, viewer);
        }
    }
}