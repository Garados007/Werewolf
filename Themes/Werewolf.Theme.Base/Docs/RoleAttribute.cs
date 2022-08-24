namespace Werewolf.Theme.Docs;

/// <summary>
/// Add this class to the list of Roles for the documentation. Adding this attribute won't change
/// any behavior of the game itself.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public sealed class RoleAttribute : System.Attribute
{
    public RoleAttribute()
    {
    }
}