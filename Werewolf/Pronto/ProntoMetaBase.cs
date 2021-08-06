namespace Werewolf.Pronto
{
    public class ProntoMetaBase
    {
        public Pronto Pronto { get; private set; }

        protected void Set<T>(ref T target, T value)
        {
            if (Equals(target, value))
                return;
            target = value;
            Changed();
        }

        private bool isEdit;
        private bool isDirty;

        protected void Changed()
        {
            if (isEdit)
            {
                isDirty = true;
            }
            else
            {
                Pronto.SendUpdate();
            }
        }

        public void BeginEdit()
        {
            isEdit = true;
        }

        public void EndEdit()
        {
            isEdit = false;
            if (isDirty)
                Changed();
            isDirty = false;
        }

        protected internal ProntoMetaBase(Pronto host)
        {
            Pronto = host;
        }

    }
}