namespace Werewolf.Theme.Default.Roles
{
    public class Villager : VillagerBase
    {
        public Villager(Theme theme) : base(theme)
        {
        }

        public override string Name => "Dorfbewohner";

        public override Role CreateNew()
        {
            return new Villager(Theme);
        }
    }
}
