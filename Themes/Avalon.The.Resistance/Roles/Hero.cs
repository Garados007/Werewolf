using Werewolf.Theme;

namespace Avalon.The.Resistance.Roles
{
    public class Hero : HeroRoleBase
    {
        public Hero(Theme theme) : base(theme)
        {
        }

        public override Role CreateNew()
            => new Hero(Theme);
    }
}