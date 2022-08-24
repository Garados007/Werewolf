namespace Werewolf.Theme.Docs;

/// <summary>
/// Add this class to mark a variable for <see cref="VoteAttribute" />. Adding this attribute won't
/// change any behavior of the game itself.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public sealed class VoteVariableAttribute : System.Attribute
{
    public string Vote { get; }

    public string Variable { get; }

    public string? Description { get; }
    
    public VoteVariableAttribute(string vote, string variable, string? description = null)
    {
        Vote = vote;
        Variable = variable;
        Description = description;
    }
}