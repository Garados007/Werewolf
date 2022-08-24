namespace Werewolf.Theme.Docs;

/// <summary>
/// Add attribute to mark the existence of an tag for the documentation. Adding this attribute won't
/// change any behavior of the game itself.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public sealed class TagAttribute : System.Attribute
{
    public string Tag { get; }

    public string? Description { get; }

    public TagAttribute(string tag, string? description = null)
    {
        Tag = tag;
        Description = description;
    }
}