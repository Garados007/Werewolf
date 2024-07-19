using System.Collections.Generic;

namespace Werewolf.Theme.Default
{
    public abstract class WerwolfBase : BaseRole
    {
        private readonly List<Roles.Girl> seenByGirl
            = new List<Roles.Girl>();
        private readonly object lockSeenByGirl = new object();

        public void AddSeenByGirl(Roles.Girl girl)
        {
            lock (lockSeenByGirl)
                seenByGirl.Add(girl);
            SendRoleInfoChanged();
        }

        public bool IsSeenByGirl(Roles.Girl girl)
        {
            lock (lockSeenByGirl)
                return seenByGirl.Contains(girl);
        }

        protected WerwolfBase(GameMode theme) : base(theme)
        {
        }

        public override bool? IsSameFaction(Role other)
        {
            return other is WerwolfBase
                ? true
                : (bool?)null;
        }

        public override Role ViewRole(Role viewer)
        {
            return viewer is WerwolfBase || (viewer is Roles.Girl girl && IsSeenByGirl(girl))
                ? new Roles.Werwolf(Theme)
                : base.ViewRole(viewer);
        }
    }
}
