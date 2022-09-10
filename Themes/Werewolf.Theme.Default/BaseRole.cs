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
            if (IsEnchantedByFlutist && (viewer == null || viewer is Roles.Flutist || (viewer is BaseRole baseRole && baseRole.IsEnchantedByFlutist)))
                yield return "enchant-flutist";
        }

        public override Role ViewRole(Role viewer)
        {
            return IsViewedByOracle && viewer is Roles.Oracle
                ? this
                : base.ViewRole(viewer);
        }
    }
}
