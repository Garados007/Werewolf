using System.Collections.Generic;

namespace Werewolf.Theme.Default
{
    public abstract class BaseRole : Role
    {
        public BaseRole(Theme theme) : base(theme)
        {
        }

        public bool IsSelectedByHealer { get; set; } = false;

        private bool isViewedByOracle = false;
        public bool IsViewedByOracle
        {
            get => isViewedByOracle;
            set
            {
                isViewedByOracle = value;
                SendRoleInfoChanged();
            }
        }

        private bool isLoved = false;
        public bool IsLoved
        {
            get => isLoved;
            set
            {
                isLoved = value;
                SendRoleInfoChanged();
            }
        }

        public bool HasVotePermitFromScapeGoat { get; set; } = false;

        private bool isEnchantedByFlutist = false;
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
            if (IsViewedByOracle && viewer is Roles.Oracle)
                return this;
            return base.ViewRole(viewer);
        }

        public virtual bool ViewLoved(Role viewer)
        {
            if (viewer is not BaseRole viewer_)
                return false;
            if (viewer_.IsLoved || viewer is Roles.Amor)
                return IsLoved;
            return false;
        }

        public override void ChangeToAboutToKill(GameRoom game)
        {
            base.ChangeToAboutToKill(game);
            if (IsLoved)
                foreach (var role in game.Participants.Values)
                    if (role is BaseRole baseRole && role != this && baseRole.IsLoved)
                        role.SetKill(game, new KillInfos.KilledByLove());
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
