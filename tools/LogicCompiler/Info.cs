namespace LogicCompiler;

internal sealed class Info
{
    public List<string> Modes { get; set; } = [];

    public List<string> PlayerNotification { get; set; } = [];

    public List<string> Labels { get; set; } = [];

    public List<string> Scenes { get; set; } = [];

    public List<string> Phases { get; set; } = [];

    public List<string> Characters { get; set; } = [];

    public Dictionary<string, SequenceInfo> Sequences { get; set; } = [];

    public Dictionary<string, VotingInfo> Votings { get; set; } = [];

    public List<string> Options { get; set; } = [];

    public List<string> Events { get; set; } = [];
}

internal sealed class SequenceInfo
{
    public List<string> Steps { get; set; } = [];
}

internal sealed class VotingInfo
{
    public List<string> UsedOptions { get; set; } = [];
}
