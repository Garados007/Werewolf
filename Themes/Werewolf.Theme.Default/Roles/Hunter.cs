namespace Werewolf.Theme.Default.Roles
{
    public class Hunter : VillagerBase
    {
        public Hunter(Theme theme) : base(theme)
        {
        }

        public bool HasKilled { get; set; } = false;

        public override string Name => "Jäger";

        public override Role CreateNew()
        {
            return new Hunter(Theme);
        }

        public override Role ViewRole(Role viewer)
        {
            if (viewer is Hunter)
                return this;
            return base.ViewRole(viewer);
        }
    }
}
