using System.Net;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using sRPC.TCP;
using System.Threading;
using System.Linq;
using Serilog;

namespace Werewolf.Game.Multiplexer
{
    public class ClientConnector : IDisposable
    {
        readonly List<TcpApiClient<Api.GameApiClient, GameNotificationServer>> connections
            = new List<TcpApiClient<Api.GameApiClient, GameNotificationServer>>();
        readonly ReaderWriterLockSlim @lock = new ReaderWriterLockSlim();
        TcpApiClient<Api.GameApiClient, GameNotificationServer>[]? enumerationBuffer = null;
        private bool disposedValue;
        readonly TcpApiServer<Api.GameNotificationClient, GameApiServer> outgoing;

        public IEnumerable<Api.GameNotificationClient> NotificationClients 
            => outgoing.RequestApis;
        
        public IEnumerable<(IPEndPoint endPoint, Api.GameApiClient api)> ApiClients
        {
            get
            {
                @lock.EnterReadLock();
                try 
                {
                    enumerationBuffer ??= connections.ToArray();
                    return enumerationBuffer.Select(x => (x.EndPoint, x.RequestApi));
                }
                finally { @lock.ExitReadLock(); }
            }
        }

        public ConcurrentDictionary<IPEndPoint, Api.ServerState> ServerStates { get; }
            = new ConcurrentDictionary<IPEndPoint, Api.ServerState>();

        public Api.ServerState MultiplexServerState
        {
            get
            {
                var state = new Api.ServerState();
                foreach (var (_, client) in ServerStates)
                {
                    state.ActiveRooms += client.ActiveRooms;
                    state.ConnectedUser += client.ConnectedUser;
                    state.MaxRooms += client.MaxRooms;
                    state.ConnectedServer += client.ConnectedServer;
                    state.ConnectedServerNames.AddRange(client.ConnectedServerNames);
                }
                return state;
            }
        }

        public ClientConnector(int outgoingPort, int timeout)
        {
            outgoing = new TcpApiServer<Api.GameNotificationClient, GameApiServer>(
                new IPEndPoint(IPAddress.Any, outgoingPort),
                x => {},
                x => x.SetConnector(this)
            );
            SyncClients(timeout);
        }

        private void SyncClients(int timeout)
        {
            Task.Run(async () => {
                while (!disposedValue)
                {
                    await Task.WhenAll(ApiClients.Select(async x =>
                    {
                        using var canceller = new CancellationTokenSource(timeout);
                        Api.ServerState? state;
                        try { state = await x.api.GetServerState(); }
                        catch (TaskCanceledException)
                        {
                            Log.Warning("The game server {endpoint} took longer than {time} ms for " +
                                "answering the request", x.endPoint, timeout);
                            return;
                        }
                        if (state != null)
                            ServerStates.AddOrUpdate(x.endPoint, state, (_, _) => state);
                    }));
                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            });
        }

        public void AddConnection(IPEndPoint endPoint, int timeout)
        {
            _ = Task.Run(async () => 
            {
                var connection = new TcpApiClient<Api.GameApiClient, GameNotificationServer>(
                    endPoint,
                    x => {},
                    x => x.SetConnector(this, endPoint, timeout)
                );
                await connection.WaitConnect.ConfigureAwait(false);
                @lock.EnterWriteLock();
                try 
                { 
                    connections.Add(connection);
                    enumerationBuffer = null;
                }
                finally { @lock.ExitWriteLock(); }
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    @lock.Dispose();
                    foreach (var connection in connections)
                        connection.Dispose();
                    outgoing.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}