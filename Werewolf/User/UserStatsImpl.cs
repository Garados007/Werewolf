using MongoDB.Driver;

namespace Werewolf.User;

public sealed class UserStatsImpl(UserInfoImpl info) : UserStats
{
    public UserInfoImpl Info { get; } = info;

    public override uint WinGames => Info.DB.Stats.WinGames;

    public override uint Killed => Info.DB.Stats.Killed;

    public override uint LooseGames => Info.DB.Stats.LooseGames;

    public override uint Leader => Info.DB.Stats.Leader;

    public override uint Level => Info.DB.Stats.Level;

    public override ulong CurrentXp => Info.DB.Stats.CurrentXp;

    public override async Task IncAsync(uint dWinGames, uint dKilled, uint dLooseGames, uint dLeader, ulong dXp)
    {
        var this_ = this;
        var idFilter = Builders<DB.UserInfo>.Filter.Eq("_id", Info.DB.Id);
        if (!Info.IsGuest) // guests are not stored in db
            try
            {
                await Info.Database.UserInfo.UpdateOneAsync(
                    idFilter,
                    Builders<DB.UserInfo>.Update
                        .Inc("Stats.WinGames", dWinGames)
                        .Inc("Stats.Killed", dKilled)
                        .Inc("Stats.LooseGames", dLooseGames)
                        .Inc("Stats.Leader", dLeader)
                        .Inc("Stats.CurrentXp", dXp)
                );
            }
            catch (System.Exception e)
            {
                Serilog.Log.Error(e, "Cannot update user");
            }
        Info.DB.Stats.WinGames += dWinGames;
        Info.DB.Stats.Killed += dKilled;
        Info.DB.Stats.LooseGames += dLooseGames;
        Info.DB.Stats.Leader += dLeader;

        if (Info.IsGuest)
            return; // guest have no level system

        Info.DB.Stats.CurrentXp += dXp;
        // Do Level Up
        using var session = await Info.Database.UserInfo.Database.Client.StartSessionAsync();
        session.StartTransaction();
        while (true)
        {
            Info.DB = await (await Info.Database.UserInfo.FindAsync(
                idFilter,
                new FindOptions<DB.UserInfo, DB.UserInfo>
                {
                    Limit = 1,
                }
            )).FirstAsync();
            if (LevelMaxXP <= CurrentXp)
            {
                try
                {
                    await Info.Database.UserInfo.UpdateOneAsync(
                        idFilter
                            & Builders<DB.UserInfo>.Filter.Eq("Stats.Level", Level)
                            & Builders<DB.UserInfo>.Filter.Eq("Stats.CurrentXp", CurrentXp),
                        Builders<DB.UserInfo>.Update
                            .Inc("Stats.Level", 1)
                            .Set("Stats.CurrentXp", CurrentXp - LevelMaxXP)
                    );
                }
                catch (System.Exception e)
                {
                    Serilog.Log.Error(e, "Cannot update user");
                }
            }
            else break;
        }
        await session.CommitTransactionAsync();
    }
}
