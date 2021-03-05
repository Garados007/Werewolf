namespace Werewolf.Theme.Default.Roles
{
    public class Oracle : VillagerBase
    {
        public Oracle(Theme theme) : base(theme)
        {
        }

        public override string Name => "alte Seherin";

        public override Role CreateNew()
        {
            return new Oracle(Theme);
        }

        public override Role ViewRole(Role viewer)
        {
            return viewer is Oracle
                ? this
                : base.ViewRole(viewer);
        }
    }
}
