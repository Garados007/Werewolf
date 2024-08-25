namespace Werewolf.User;

public sealed class UserInfoImpl : UserInfo
{
    public DB.UserInfo DB { get; internal set; }

    public Database Database { get; }

    public override UserId Id => new UserId(DB.Id);

    public override string? OAuthId => DB.OAuthId;

    public override UserConfig Config { get; }

    public override UserStats Stats { get; }

    public UserInfoImpl(Database database, DB.UserInfo db)
    {
        Database = database;
        DB = db;
        Config = new UserConfigImpl(this);
        Stats = new UserStatsImpl(this);
    }
}
