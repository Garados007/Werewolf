using System;

namespace Werewolf.Pronto
{
    public class ProntoJoinToken
    {
        public string Token { get; }

        public DateTime AliveUntil { get; }

        public ProntoJoinToken(string token, DateTime aliveUntil)
        {
            Token = token;
            AliveUntil = aliveUntil;
        }

        public bool Invalid => AliveUntil < DateTime.Now;
    }
}