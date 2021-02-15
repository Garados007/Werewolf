namespace Werewolf.Theme.Default
{
    public abstract class VillagerBase : BaseRole
    {
        protected VillagerBase(Theme theme) : base(theme)
        {
        }

        public override bool? IsSameFaction(Role other)
        {
            if (other is VillagerBase)
                return true;
            return null;
        }
    }
}
