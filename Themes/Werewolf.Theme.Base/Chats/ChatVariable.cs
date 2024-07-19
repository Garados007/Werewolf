namespace Werewolf.Theme.Chats;

public readonly struct ChatVariable
{
    readonly string? text;
    public string Text => text ?? "";

    public ReadOnlyMemory<string> Data { get; }
    public Dictionary<string, string>? Args { get; }

    public ChatVariableType Type { get; }

    public ChatVariable(ChatVariableType type, string text)
    {
        Type = type;
        this.text = text;
        Data = ReadOnlyMemory<string>.Empty;
        Args = null;
    }

    public ChatVariable(ChatVariableType type, string text, ReadOnlyMemory<string> data,
        Dictionary<string, string> args
    )
    {
        Type = type;
        this.text = text;
        Data = data;
        Args = args;
    }

    public ChatVariable(string text)
        : this(ChatVariableType.Plain, text)
    { }

    public static implicit operator ChatVariable(string text)
        => new ChatVariable(text);

    public static implicit operator ChatVariable(User.UserId user)
        => new ChatVariable(ChatVariableType.User, user.ToString());

    public static implicit operator ChatVariable(Voting voting)
        => new ChatVariable(ChatVariableType.Voting, voting.LanguageId);

    public static implicit operator ChatVariable((Voting voting, VoteOption option) data)
        => new ChatVariable(
            ChatVariableType.VotingOption,
            data.option.LangId,
            new[] { data.voting.LanguageId },
            data.option.Vars
        );

    public static implicit operator ChatVariable(Phase phase)
        => new ChatVariable(ChatVariableType.Phase, phase.LanguageId);
}
