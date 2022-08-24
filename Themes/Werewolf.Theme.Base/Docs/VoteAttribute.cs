namespace Werewolf.Theme.Docs;

/// <summary>
/// Add this class to the list the vote option to the linked voting. Adding this attribute won't
/// change any behavior of the game itself. <br/>
/// This attribute is always inherited by the documentation generator.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class VoteAttribute : System.Attribute
{
    public string Vote { get; }

    public string? Description { get; }

    /// <summary>
    /// Set this value to true to remove the vote from the documentation again. This can be used if
    /// the base class has added this attribute but the child class doesn't use it anymore.
    /// </summary>
    public bool Remove { get; set; }
    
    public VoteAttribute(string vote, string? description = null)
    {
        Vote = vote;
        Description = description;
    }
}
