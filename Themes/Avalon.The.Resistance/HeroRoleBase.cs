using Werewolf.Theme;

namespace Avalon.The.Resistance
{
    public abstract class HeroRoleBase : BaseRole
    {
        public HeroRoleBase(Theme theme) : base(theme)
        {
        }

        public override bool? IsSameFaction(Role other)
        {
            return other is HeroRoleBase;
        }
    }
}