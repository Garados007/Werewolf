namespace Werewolf.Theme.Default.Roles
{
    public class Healer : VillagerBase
    {
        public Healer(Theme theme) : base(theme)
        {
        }

        public override string Name => "Heiler";

        public override Role CreateNew()
        {
            return new Healer(Theme);
        }
    }
}
