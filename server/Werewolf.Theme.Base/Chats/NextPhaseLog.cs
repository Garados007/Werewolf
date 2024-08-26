namespace Werewolf.Theme.Chats;

public class NextPhaseLog : ChatServiceMessage
{
    public Scene Phase { get; }

    public NextPhaseLog(Scene phase)
        => Phase = phase;

    public override bool Epic => true;

    public override bool CanSendTo(GameRoom game, User.UserInfo user)
        => true;

    public override IEnumerable<(string key, ChatVariable value)> GetArgs()
    {
        yield return ("phase", Phase);
    }
}
