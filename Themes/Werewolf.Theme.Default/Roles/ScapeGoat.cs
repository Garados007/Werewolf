namespace Werewolf.Theme.Default.Roles
{
    public class ScapeGoat : VillagerBase
    {
        public bool WasKilledByVillage { get; set; }

        public bool HasRevenge { get; set; }

        public bool HasDecided { get; set; }

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
