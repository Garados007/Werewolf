namespace Werewolf.Theme.Default.Roles
{
    public class ThreeBrothers : VillagerBase
    {
        public ThreeBrothers(GameMode theme) : base(theme)
        {
        }

        public bool HasSeenPartner { get; set; }

        public override Role ViewRole(Role viewer)
        {
            if (viewer is ThreeBrothers && HasSeenPartner)
                return this;
            return base.ViewRole(viewer);
        }

        public override string Name => "ThreeBrothers";

        public override Role CreateNew()
            => new ThreeBrothers(Theme);
    }
}
