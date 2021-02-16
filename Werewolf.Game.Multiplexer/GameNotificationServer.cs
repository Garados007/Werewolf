using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werewolf.Game.Api;
using Serilog;

namespace Werewolf.Game.Multiplexer
{
    public class GameNotificationServer : GameNotificationServerBase
    {
        private ClientConnector? connector;
        private IPEndPoint? endPoint;
        private int timeout = 5000;

        public void SetConnector(ClientConnector connector, IPEndPoint endPoint, int timeout)
        {
            this.connector = connector;
            this.endPoint = endPoint;
            this.timeout = timeout;
        }

        public override Task RoomUpdated(GameRoom request, CancellationToken cancellationToken)
        {
            if (connector == null)
                return Task.CompletedTask;
            _ = Task.Run(async () => await Task.WhenAll(connector.NotificationClients.Select(
                async x => 
                {
                    using var source = new CancellationTokenSource(timeout);
                    var combined = CancellationTokenSource.CreateLinkedTokenSource(
                        source.Token, cancellationToken);
                    try { await x.RoomUpdated(request, combined.Token); }
                    catch (TaskCanceledException)
                    {
                        if (source.IsCancellationRequested)
                        {
                            Log.Warning("A connected api client took longer than {time} ms for " +
                                "answering the request", timeout);
                        }
                    }
                }
            )), CancellationToken.None);
            return Task.CompletedTask;
        }

        public override Task ServerUpdated(ServerState request, CancellationToken cancellationToken)
        {
            if (connector == null || endPoint == null)
                return Task.CompletedTask;
            connector.ServerStates.AddOrUpdate(endPoint, request, (_, _) => request);
            var state = connector.MultiplexServerState;
            _ = Task.Run(async () => await Task.WhenAll(connector.NotificationClients.Select(
                async x => 
                {
                    using var source = new CancellationTokenSource(timeout);
                    var combined = CancellationTokenSource.CreateLinkedTokenSource(
                        source.Token, cancellationToken);
                    try { await x.ServerUpdated(state, cancellationToken); }
                    catch (TaskCanceledException)
                    {
                        if (source.IsCancellationRequested)
                        {
                            Log.Warning("A connected api client took longer than {time} ms for " +
                                "answering the request", timeout);
                        }
                    }
                }
            )), CancellationToken.None);
            return Task.CompletedTask;
        }
    }
}