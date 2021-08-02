namespace Werewolf.Game.Pronto
{
    public class ProntoServer : ProntoMetaBase
    {
        private string name = "";
        public string Name
        {
            get => name;
            set => Set(ref name, value ?? "");
        }

        private string uri = "";
        public string Uri
        {
            get => uri;
            set => Set(ref uri, value ?? "");
        }

        private bool developer = false;
        public bool Developer
        {
            get => developer;
            set => Set(ref developer, value);
        }

        private bool fallback = false;
        public bool Fallback
        {
            get => fallback;
            set => Set(ref fallback, value);
        }

        private bool full = false;
        public bool Full
        {
            get => full;
            set => Set(ref full, value);
        }

        private bool maintenance = false;
        public bool Maintenance
        {
            get => maintenance;
            set => Set(ref maintenance, value);
        }

        private int? maxClients = null;
        public int? MaxClients
        {
            get => maxClients;
            set => Set(ref maxClients, value);
        }

        internal ProntoServer(Pronto host)
            : base(host)
        {
        }
    }
}