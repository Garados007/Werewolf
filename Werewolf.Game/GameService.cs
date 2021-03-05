using MaxLib.WebServer;
using System.Threading.Tasks;

namespace Werewolf.Game
{
    public class GameService : WebService
    {
        public GameService()
            : base(ServerStage.ParseRequest)
        {
        }

        public override bool CanWorkWith(WebProgressTask task)
        {
            return task.Request.Location.StartsUrlWith(new[] { "game" })
                && task.Request.Location.DocumentPathTiles.Length == 2;
        }

        public override Task ProgressTask(WebProgressTask task)
        {
            task.Request.Location.SetLocation("content/index.html");
            task.NextStage = ServerStage.ParseRequest;
            return Task.CompletedTask;
        }
    }
}
