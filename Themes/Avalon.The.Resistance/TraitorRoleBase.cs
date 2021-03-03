using Werewolf.Theme;

namespace Avalon.The.Resistance
{
    public abstract class TraitorRoleBase : BaseRole
    {
        protected TraitorRoleBase(Theme theme) : base(theme)
        {
        }

        public override bool? IsSameFaction(Role other)
        {
            return other is TraitorRoleBase;
        }

        public override Role ViewRole(Role viewer)
        {
            if (viewer is TraitorRoleBase || viewer is Roles.Merlin)
                return new Roles.Traitor(Theme);
            return base.ViewRole(viewer);
        }

    }
}