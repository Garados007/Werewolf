using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Default
{
    public abstract class BaseRole : Role
    {
        protected BaseRole(GameMode theme) : base(theme)
        {
            Effects.Add(new Default.Effects.BeforeKillAction.LogPlayerKill());
        }

        public bool IsSelectedByHealer { get; set; }

        public override IEnumerable<string> GetTags(GameRoom game, Role? viewer)
        {
            foreach (var tag in base.GetTags(game, viewer))
                yield return tag;
        }

        public override Role ViewRole(Role viewer)
        {
            var trueShown = Effects.GetEffect<Effects.TrueIdentityShownEffect>(
                x => x.Viewer == viewer
            ) is not null;
            if (trueShown)
                return this;
            return base.ViewRole(viewer);
        }
    }
}
