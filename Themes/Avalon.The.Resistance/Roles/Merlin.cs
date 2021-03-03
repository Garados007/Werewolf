using Werewolf.Theme;

namespace Avalon.The.Resistance.Roles
{
    public class Merlin : HeroRoleBase
    {
        public Merlin(Theme theme) : base(theme)
        {
        }

        public override Role CreateNew()
            => new Merlin(Theme);
    }
}