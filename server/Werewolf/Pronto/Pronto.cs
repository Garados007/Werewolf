using System.Text.Json;

namespace Werewolf.Pronto;

public class Pronto : IDisposable
{
    public ProntoConfig Config { get; private set; }

    public string? Id { get; private set; }

    private readonly Timer timer;

    public Pronto(ProntoConfig config)
    {
        BeginEdit();
        Config = config;
        server = new ProntoServer(this)
        {
            Developer = config.Developer,
            Fallback = config.Fallback,
            MaxClients = config.MaxClients,
        };
        games = new Dictionary<string, ProntoGame>();
        dirty = false;
        EndEdit();
        timer = new Timer(
            _ => SendUpdate(),
            null,
            Config.KeepAliveInterval,
            Config.KeepAliveInterval
        );
    }

    private readonly ProntoServer server;
    private readonly Dictionary<string, ProntoGame> games;

    public ProntoServer GetServer()
        => server;

    public ProntoGame GetGame(string id)
    {
        if (!games.TryGetValue(id, out ProntoGame? game))
            games.Add(id, game = new ProntoGame(this, id)
            {
                MaxRooms = Config.MaxRooms,
            });
        return game;
    }

    public void RemoveGame(string id)
    {
        games.Remove(id);
    }

    private int locked;
    private bool dirty;

    private DateTime lockedUntil = DateTime.MinValue;

    public void BeginEdit()
    {
        locked = 1;
    }

    public void EndEdit()
    {
        locked = 0;
        if (dirty)
        {
            SendUpdate();
        }
    }

    public event Action<Pronto>? OnBeforeSendUpdate;

    public void SendUpdate()
    {
        dirty = true;
        if (Interlocked.Exchange(ref locked, 1) != 0)
            return;
        try
        {

            var now = DateTime.Now;
            var until = lockedUntil;
            if (now < until)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(lockedUntil - now).CAF();
                    if (lockedUntil == until)
                        SendUpdate();
                });
                return;
            }
            lockedUntil = now + Config.NotifyCooldown;

            OnBeforeSendUpdate?.Invoke(this);
            UploadStatus().Wait();
        }
        finally
        {
            dirty = false;
            locked = 0;
        }
    }

    private byte[] WriteToJson()
    {
        using var m = new MemoryStream();
        using var w = new Utf8JsonWriter(m);
        w.WriteStartObject();
        w.WriteString("name", server.Name);
        w.WriteString("uri", server.Uri);
        w.WriteBoolean("developer", server.Developer);
        w.WriteBoolean("fallback", server.Fallback);
        w.WriteBoolean("full", server.Full);
        w.WriteBoolean("maintenance", server.Maintenance);
        if (server.MaxClients is null)
            w.WriteNull("max-clients");
        else w.WriteNumber("max-clients", server.MaxClients.Value);
        w.WriteStartArray("games");
        foreach (var (_, game) in games)
        {
            w.WriteStartObject();
            w.WriteString("name", game.Id);
            w.WriteString("uri", game.Uri);
            w.WriteNumber("rooms", game.Rooms);
            if (game.MaxRooms is null)
                w.WriteNull("max-rooms");
            else w.WriteNumber("max-rooms", game.MaxRooms.Value);
            w.WriteNumber("clients", game.Clients);
            w.WriteEndObject();
        }
        w.WriteEndArray();
        w.WriteEndObject();
        w.Flush();
        return m.ToArray();
    }

    private async Task UploadStatus()
    {
        using var hc = new System.Net.Http.HttpClient();
        hc.DefaultRequestHeaders.Add("token", Config.Token);
        JsonDocument json;
        try
        {
            using var result = (
                await hc.PostAsync(
                    $"{Config.Url}/v1/update",
                    new System.Net.Http.ByteArrayContent(WriteToJson())
                    {
                        Headers =
                        {
                            { "Content-Type", "application/json" }
                        }
                    }
                ).CAF()
            ).Content.ReadAsStream();
            json = await JsonDocument.ParseAsync(result).CAF();
        }
        catch (System.Net.WebException e)
        {
            Serilog.Log.Error(e, "Cannot upload server status");
            return;
        }
        catch (System.Text.Json.JsonException e)
        {
            Serilog.Log.Error(e, "Cannot upload server status");
            return;
        }
        var oldId = Id;
        if (json.RootElement.TryGetProperty("id", out JsonElement idElement))
            Id = idElement.GetString();
        if (oldId != Id)
            Serilog.Log.Information("Pronto: Server Id is {id}", Id);
    }

    public async Task<ProntoJoinToken?> CreateToken(string game, string lobby)
    {
        using var hc = new System.Net.Http.HttpClient();
        hc.DefaultRequestHeaders.Add("token", Config.Token);

        using var s = new MemoryStream();
        using var w = new Utf8JsonWriter(s);
        w.WriteStartObject();
        w.WriteString("game", game);
        w.WriteString("lobby", lobby);
        w.WriteEndObject();
        w.Flush();
        s.Position = 0;

        JsonDocument json;
        try
        {
            using var result = (
                await hc.PostAsync(
                    $"{Config.Url}/v1/token",
                    new System.Net.Http.StreamContent(s)
                    {
                        Headers =
                        {
                            { "Content-Type", "application/json" }
                        }
                    }
                ).CAF()
            ).Content.ReadAsStream();
            json = await JsonDocument.ParseAsync(result).CAF();
        }
        catch (System.Net.WebException e)
        {
            Serilog.Log.Error(e, "Cannot fetch token");
            return null;
        }

        var token = json.RootElement.TryGetProperty("token", out JsonElement node)
            ? node.GetString() : null;
        if (token is null)
            return null;
        // In the pronto specification are 15 minutes as live span stated.
        // The pronto server itself will discard tokens after 20 minutes.
        return new ProntoJoinToken(
            token,
            DateTime.UtcNow + TimeSpan.FromMinutes(15)
        );
    }

    public void Dispose()
    {
        timer.Dispose();
    }
}
