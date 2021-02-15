namespace Werewolf.Theme.Default.Roles
{
    public class PureSoul : VillagerBase
    {
        public PureSoul(Theme theme) : base(theme)
        {
        }

        public override Role ViewRole(Role viewer)
        {
            return this;
        }

        public override string Name => "pure-soul";

        public override Role CreateNew()
            => new PureSoul(Theme);
    }
}
