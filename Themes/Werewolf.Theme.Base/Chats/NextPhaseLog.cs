namespace Werewolf.Theme.Chats;

public class NextPhaseLog : ChatServiceMessage
{
    public Phase Phase { get; }

    public NextPhaseLog(Phase phase)
        => Phase = phase;

    public override bool Epic => true;

    public override bool CanSendTo(GameRoom game, User.UserInfo user)
        => true;

    public override IEnumerable<(string key, ChatVariable value)> GetArgs()
    {
        yield return ("phase", Phase);
    }
}