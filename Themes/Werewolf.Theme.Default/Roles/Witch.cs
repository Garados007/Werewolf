namespace Werewolf.Theme.Default.Roles
{
    public class Witch : VillagerBase
    {
        public bool UsedLivePotion { get; set; }

        public bool UsedDeathPotion { get; set; }

        public Witch(Theme theme) : base(theme)
        {
        }


        public override string Name => "Hexe";

        public override Role CreateNew()
        {
            return new Witch(Theme);
        }

        public override Role ViewRole(Role viewer)
        {
            if (viewer is Witch)
                return this;
            return base.ViewRole(viewer);
        }
    }
}
