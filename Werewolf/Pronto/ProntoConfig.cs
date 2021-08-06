using System;
using MaxLib.Ini;

namespace Werewolf.Pronto
{
    public class ProntoConfig
    {
        public string Url { get; init; } = "";

        public string Token { get; init; } = "";

        public bool Developer { get; init; }

        public bool Fallback { get; init; }

        public int? MaxClients { get; init; }

        public int? MaxRooms { get; init; }

        public TimeSpan KeepAliveInterval { get; init; }

        public TimeSpan NotifyCooldown { get; init; }

        public ProntoConfig() {}

        public ProntoConfig(IniGroup ini)
        {
            Url = ini.GetString("url", "");
            Token = ini.GetString("token", "");
            Developer = ini.GetBool("developer", false);
            Fallback = ini.GetBool("fallback", false);
            MaxClients = ini.GetBool("max-clients-enabled", false)
                ? ini.GetInt32("max-clients", int.MaxValue)
                : null;
            MaxRooms = ini.GetBool("max-rooms-enabled", false)
                ? ini.GetInt32("max-rooms", int.MaxValue)
                : null;
            KeepAliveInterval = TimeSpan.FromSeconds(
                ini.GetDouble("keep-alive-interval", 30)
            );
            NotifyCooldown = TimeSpan.FromMilliseconds(
                ini.GetDouble("notify-cooldown", 500)
            );
        }
    }
}