using Werewolf.Theme;

namespace Avalon.The.Resistance.Roles
{
    public class Assassin : TraitorRoleBase
    {
        public Assassin(Theme theme) : base(theme)
        {
        }

        public override Role CreateNew()
            => new Assassin(Theme);
    }
}