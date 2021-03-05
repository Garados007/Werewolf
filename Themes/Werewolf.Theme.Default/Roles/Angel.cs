namespace Werewolf.Theme.Default.Roles
{
    public class Angel : VillagerBase
    {
        public bool MissedFirstRound { get; set; }

        public Angel(Theme theme) : base(theme)
        {
        }

        public override string Name => "Angel";

        public override Role CreateNew()
        {
            return new Angel(Theme);
        }
    }
}
