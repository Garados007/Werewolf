namespace Werewolf.Game.Pronto
{
    public class ProntoGame : ProntoMetaBase
    {
        public string Id { get; }

        private string uri = "";
        public string Uri
        {
            get => uri;
            set => Set(ref uri, value ?? "");
        }

        private int rooms;
        public int Rooms
        {
            get => rooms;
            set => Set(ref rooms, value);
        }

        private int? maxRooms;
        public int? MaxRooms
        {
            get => maxRooms;
            set => Set(ref maxRooms, value);
        }

        private int clients;
        public int Clients
        {
            get => clients;
            set => Set(ref clients, value);
        }

        internal ProntoGame(Pronto host, string id)
            : base(host)
        {
            Id = id;
        }
    }
}