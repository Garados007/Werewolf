namespace Werewolf.Theme.Labels;

/// <summary>
/// This is the base interface for all <see cref="ILabel"/> that can be attached to <see
/// cref="GameRoom" />, <see cref="Scene" />, <see cref="GameUserEntry" />, <see cref="Voting" /> or
/// <see cref="Scene" />.
/// </summary>
public interface ILabel
{
}

public interface ILabelHost<T>
    where T : class, ILabel
{
    LabelCollection<T> Labels { get; }
}
