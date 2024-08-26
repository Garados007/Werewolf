namespace Werewolf.Pronto;

public class ProntoJoinToken(string token, DateTime aliveUntil)
{
    public string Token { get; } = token;

    public DateTime AliveUntil { get; } = aliveUntil;

    public bool Invalid => AliveUntil < DateTime.Now;
}
