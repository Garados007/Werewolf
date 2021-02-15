namespace Werewolf.Theme.Default.Roles
{
    public class Idiot : VillagerBase
    {
        private bool isRevealed = false;
        public bool IsRevealed
        {
            get => isRevealed;
            set
            {
                isRevealed = value;
                SendRoleInfoChanged();
            }
        }

        public bool WasMajor { get; set; }

        public Idiot(Theme theme) : base(theme)
        {
        }

        public override Role ViewRole(Role viewer)
        {
            if (IsRevealed)
                return this;
            return base.ViewRole(viewer);
        }

        public override string Name => "Dorfdepp";

        public override Role CreateNew()
        {
            return new Idiot(Theme);
        }
    }
}
