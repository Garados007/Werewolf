using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Default
{
    public abstract class BaseRole : Role
    {
        protected BaseRole(Theme theme) : base(theme)
        {
            Effects.Add(new Default.Effects.BeforeKillAction.LogPlayerKill());
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
                if (isLoved)
                    Effects.Add(new Default.Effects.BeforeKillAction.KillByLove());
                else Effects.Remove<Default.Effects.BeforeKillAction.KillByLove>();
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
    }
}
