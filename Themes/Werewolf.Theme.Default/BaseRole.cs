using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Default
{
    public abstract class BaseRole : Role
    {
        protected BaseRole(Theme theme) : base(theme)
        {
        }

        public bool IsSelectedByHealer { get; set; }

        private bool isViewedByOracle;
        public bool IsViewedByOracle
        {
            get => isViewedByOracle;
            set
            {
                isViewedByOracle = value;
                SendRoleInfoChanged();
            }
        }

        private bool isLoved;
        public bool IsLoved
        {
            get => isLoved;
            set
            {
                isLoved = value;
                SendRoleInfoChanged();
            }
        }

        public bool HasVotePermitFromScapeGoat { get; set; }

        private bool isEnchantedByFlutist;
        public bool IsEnchantedByFlutist
        {
            get => isEnchantedByFlutist;
            set
            {
                isEnchantedByFlutist = value;
                SendRoleInfoChanged();
            }
        }

        public override IEnumerable<string> GetTags(GameRoom game, Role? viewer)
        {
            foreach (var tag in base.GetTags(game, viewer))
                yield return tag;
            if (IsLoved && (viewer == this || viewer == null || ViewLoved(viewer)))
                yield return "loved";
            if (IsEnchantedByFlutist && (viewer == null || viewer is Roles.Flutist || (viewer is BaseRole baseRole && baseRole.IsEnchantedByFlutist)))
                yield return "enchant-flutist";
        }

        public override Role ViewRole(Role viewer)
        {
            return IsViewedByOracle && viewer is Roles.Oracle
                ? this
                : base.ViewRole(viewer);
        }

        public virtual bool ViewLoved(Role viewer)
        {
            return viewer is BaseRole viewer_
                && (viewer_.IsLoved || viewer is Roles.Amor)
                && IsLoved;
        }

        public override void ChangeToAboutToKill(GameRoom game)
        {
            base.ChangeToAboutToKill(game);
            if (IsLoved)
                foreach (var role in game.Users.Select(x => x.Value.Role))
                    if (role is BaseRole baseRole && role != this && baseRole.IsLoved)
                        role.SetKill(game, new KillInfos.KilledByLove());
            if (game.TryGetId(this) is User.UserId id)
                game.SendChat(new Chats.PlayerKillLog(id));
        }

        //public override void Kill(GameRoom game)
        //    => Kill(game, true);

        //private void Kill(GameRoom game, bool checkLoved)
        //{
        //    IsAboutToBeKilled = true;
        //    if (IsLoved && checkLoved)
        //        foreach (var role in game.AliveRoles)
        //            if (role is BaseRole baseRole && baseRole.IsLoved)
        //                baseRole.Kill(game, false);
        //}

        //private void RealKill(GameRoom game)
        //    => base.Kill(game);

        //public void RealKill(GameRoom game, string? notificationId, out IEnumerable<ulong> victims)
        //{
        //    var victims_ = new HashSet<ulong>();
        //    ulong? id;
        //    if ((id = game.TryGetId(this)) != null)
        //        victims_.Add(id.Value);
        //    if (IsLoved)
        //        foreach (var (pid, role) in game.Participants)
        //            if (role is BaseRole baseRole && baseRole.IsLoved)
        //            {
        //                victims_.Add(pid);
        //                baseRole.RealKill(game);
        //                baseRole.IsAboutToBeKilled = false;
        //            }
        //    IsAboutToBeKilled = false;
        //    base.Kill(game);
        //    if (notificationId != null)
        //        game.SendEvent(new Events.PlayerNotification(notificationId, victims_.ToArray()));
        //    victims = victims_;
        //}
    }
}
