using Werewolf.Theme;

namespace Avalon.The.Resistance.Roles
{
    public class Traitor : TraitorRoleBase
    {
        public Traitor(Theme theme) : base(theme)
        {
        }

        public override Role CreateNew()
            => new Traitor(Theme);
    }
}