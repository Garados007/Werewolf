namespace Werewolf.Theme.Default.Roles
{
    public class TwoSisters : VillagerBase
    {
        public TwoSisters(Theme theme) : base(theme)
        {
        }

        public bool HasSeenPartner { get; set; }

        public override Role ViewRole(Role viewer)
        {
            if (viewer is TwoSisters && HasSeenPartner)
                return this;
            return base.ViewRole(viewer);
        }

        public override string Name => "TwoSisters";

        public override Role CreateNew()
            => new TwoSisters(Theme);
    }
}
