namespace Werewolf.Theme.Default.Roles
{
    public class Witch : VillagerBase
    {
        public bool UsedLivePotion { get; set; }

        public bool UsedDeathPotion { get; set; }

        public Witch(GameMode theme) : base(theme)
        {
        }


        public override string Name => "Hexe";

        public override Role CreateNew()
        {
            return new Witch(Theme);
        }

        public override Role ViewRole(Role viewer)
        {
            return viewer is Witch
                ? this
                : base.ViewRole(viewer);
        }
    }
}
