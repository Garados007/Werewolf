using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.User;

namespace Werewolf.Theme
{
    /// <summary>
    /// The basic role every game user has. Any special state is encoded in this role.
    /// </summary>
    public abstract class Role
    {
        public Effects.EffectCollection<Effects.IRoleEffect> Effects { get; } = new();

        private KillState killState = KillState.Alive;
        public KillState KillState
        {
            get => killState;
            private set
            {
                killState = value;
                SendRoleInfoChanged();
            }
        }

        public bool IsAlive => killState switch
        {
            KillState.Alive => true,
            KillState.MarkedKill => true,
            KillState.AboutToKill => false,
            KillState.BeforeKill => false,
            KillState.Killed => false,
            _ => true,
        };

        public KillInfo? KillInfo { get; private set; }

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
            if (KillInfo != null)
                foreach (var info in KillInfo.GetKillFlags(game, viewer))
                    yield return info;
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
        /// Set <see cref="KillState"/> to <see cref="KillState.MarkedKill"/>. This role
        /// is now marked to be killed. This can be undone with <see cref="RemoveKillFlag"/>.
        /// </summary>
        /// <param name="info">the kill info</param>
        public void AddKillFlag(KillInfo info)
        {
            if (KillState != KillState.Alive)
                return;
            KillInfo = info;
            KillState = KillState.MarkedKill;
        }

        /// <summary>
        /// Change the <see cref="KillState"/> back to <see cref="KillState.Alive"/>. This
        /// works only if it was <see cref="KillState.MarkedKill"/>.
        /// </summary>
        public void RemoveKillFlag()
        {
            if (KillState != KillState.MarkedKill)
                return;
            KillInfo = null;
            KillState = KillState.Alive;
        }

        /// <summary>
        /// Mark this role directly with <see cref="KillState.AboutToKill"/>. This skips the 
        /// flagging the kill. To work this requires the <see cref="KillState"/> to be at
        /// <see cref="KillState.Alive"/> or <see cref="KillState.MarkedKill"/>.
        /// <br/>
        /// This will implicitly call <see cref="ChangeToAboutToKill(GameRoom)"/>.
        /// </summary>
        /// <param name="game">The current game</param>
        /// <param name="info">the new kill info</param>
        public void SetKill(GameRoom game, KillInfo info)
        {
            if ((int)KillState >= (int)KillState.AboutToKill)
                return;
            killState = KillState.MarkedKill;
            KillInfo = info;
            ChangeToAboutToKill(game);
        }

        /// <summary>
        /// Transition the <see cref="KillState"/> from <see cref="KillState.MarkedKill"/> to
        /// <see cref="KillState.AboutToKill"/>. This step can also execute custom code to
        /// kill other roles.
        /// </summary>
        /// <param name="game">the linked game</param>
        public virtual void ChangeToAboutToKill(GameRoom game)
        {
            if (KillState != KillState.MarkedKill)
                return;
            KillState = KillState.AboutToKill;
        }

        /// <summary>
        /// Transition the <see cref="KillState"/> from <see cref="KillState.AboutToKill"/> to
        /// <see cref="KillState.BeforeKill"/>. After this the role can do some last steps.
        /// </summary>
        /// <param name="game">the linked game.</param>
        public virtual void ChangeToBeforeKill(GameRoom game)
        {
            if (KillState != KillState.AboutToKill)
                return;
            KillState = KillState.BeforeKill;
        }

        /// <summary>
        /// Transition the <see cref="KillState"/> from <see cref="KillState.BeforeKill"/> to
        /// <see cref="KillState.Killed"/>. The role is now considered to be finally dead and 
        /// no more actions are done.
        /// </summary>
        public void ChangeToKilled()
        {
            if (KillState != KillState.BeforeKill)
                return;
            KillState = KillState.Killed;
            KillInfo = null;
        }

        public static Role? GetSeenRole(GameRoom game, uint? round, UserInfo user, UserId targetId, Role target)
        {
            var ownRole = game.TryGetRole(user.Id);
            return (game.Leader == user.Id && !game.LeaderIsPlayer) ||
                    targetId == user.Id ||
                    round == game.ExecutionRound ||
                    (game.AllCanSeeRoleOfDead && !target.IsAlive) ||
                    (ownRole != null && game.DeadCanSeeAllRoles && ownRole.KillState == KillState.Killed) ?
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