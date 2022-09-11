namespace Werewolf.Theme.Default.Roles
{
    public class ScapeGoat : VillagerBase
    {
        public bool TakingRevenge { get; set; }

        public ScapeGoat(Theme theme) : base(theme)
        {
        }

        public override string Name => "Sündenbock";

        public override Role CreateNew()
        {
            return new ScapeGoat(Theme);
        }
    }
}
