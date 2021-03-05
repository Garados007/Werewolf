namespace Werewolf.Users.Api
{
    public partial class UserId
    {
        public static bool operator ==(UserId left, UserId right)
            => Equals(left, right);

        public static bool operator !=(UserId left, UserId right)
            => !Equals(left, right);
    }
}