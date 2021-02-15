namespace Werewolf.Theme.Default.Roles
{
    public class TwoSisters : VillagerBase
    {
        public TwoSisters(Theme theme) : base(theme)
        {
        }

        public override string Name => "TwoSisters";

        public override Role CreateNew()
            => new TwoSisters(Theme);
    }
}
