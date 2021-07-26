using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MaxLib.WebServer.WebSocket;
using Werewolf.Theme;

namespace Werewolf.Game
{
    public class GameWebSocketConnection : EventConnection
    {
        public GameRoom Game { get; }

        public GameUserEntry UserEntry { get; }

        public UserFactory UserFactory { get; }

        public GameWebSocketConnection(Stream networkStream, EventFactory factory, 
            UserFactory userFactory, GameRoom game, GameUserEntry entry)
            : base(networkStream, factory)
        {
            Game = game;
            UserEntry = entry;
            UserFactory = userFactory;
            GameController.Current.AddWsConnection(this);
            entry.AddConnection();
            Closed += (_, __) =>
            {
                GameController.Current.RemoveWsConnection(this);
                entry.RemoveConnection();
            };
        }

        protected override Task ReceiveClose(CloseReason? reason, string? info)
        {
            return Task.CompletedTask;
        }

        public async Task SendEvent(GameEvent @event)
        {
            await SendFrame(new Events.SubmitGameEvents(@event, Game, UserEntry.User));
        }

        protected override Task ReceivedFrame(EventBase @event)
        {
            _ = Task.Run(async () => 
            {
                switch (@event)
                {
                    case Events.FetchRoles fetchRoles:
                        await Handle(fetchRoles).CAF();
                        break;
                    case Events.RequestGameData:
                        await SendFrame(
                            new Events.SendGameData(Game, UserEntry.User, UserFactory)
                        ).CAF();
                        break;
                    case Events.SetGameConfig setGameConfig:
                        await SendResult(setGameConfig, Handle(setGameConfig));
                        break;
                    case Events.SetUserConfig setUserConfig:
                        await SendResult(setUserConfig, Handle(setUserConfig));
                        break;
                }
            });
            return Task.CompletedTask;
        }

        private async Task SendResult(Events.TaggedEvent? @event, string? error, 
            bool sendSuccess = true
        )
        {
            if (error is not null)
                await SendFrame(new Events.ErrorMessage
                {
                    Tag = @event?.Tag,
                    Message = error,
                });
            else if (sendSuccess)
                await SendFrame(new Events.Success
                {
                    Tag = @event?.Tag,
                });
        }

        private async Task Handle(Events.FetchRoles fetchRoles)
        {
            var send = new Events.SubmitRoles();
            var theme = new Werewolf.Theme.Default.DefaultTheme(null, UserFactory);
            var list = new List<string>();
            send.Roles.Add(theme.GetType().FullName ?? "", list);
            foreach (var template in theme.GetRoleTemplates())
                list.Add(template.GetType().Name);
            await SendFrame(send);
        }

        private string? Handle(Events.SetGameConfig gameConfig)
        {
            if (UserEntry.User.Id != Game.Leader)
                return "you are not the leader";
            if (Game.Phase is not null)
                return "cannot change settings of running game";
                
            if (gameConfig.Leader is not null && !Game.Users.ContainsKey(gameConfig.Leader))
                return "new leader is not a member of the group";
            
            var known = (Game.Theme?.GetRoleTemplates() ?? Enumerable.Empty<Role>())
                .ToDictionary(x => x.GetType().Name);
            Dictionary<Role, int>? roleConfig = null;
            if (gameConfig.RoleConfig is not null)
            {
                roleConfig = new Dictionary<Role, int>();
                foreach (var (key, count) in gameConfig.RoleConfig)
                {
                    if (!known.TryGetValue(key, out Role? role))
                        return $"unknown role '{role}'";
                    if (count < 0)
                        return $"invalid number for role '{key}'";
                    var newCount = count;

                    if (!Game.Theme!.CheckRoleUsage(
                        role: role, 
                        count: ref newCount, 
                        oldCount: Game.RoleConfiguration.TryGetValue(role, out int oldValue) 
                            ? oldValue : 0,
                        error: out string? error))
                        return error;
                    
                    roleConfig.Add(role, newCount);
                }
            }

            Theme.Theme? theme = null;
            if (gameConfig.Theme is not null)
            {
                static Type? LoadType(string name)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var type = assembly.GetType(name, false, true);
                        if (type != null)
                            return type;
                    }
                    return null;
                }
                var type = LoadType(gameConfig.Theme);
                if (type is null)
                    return $"theme implementation {gameConfig.Theme} not found";
                if (!type.IsSubclassOf(typeof(Theme.Theme)))
                    return $"theme implementation {gameConfig.Theme} is not a valid theme";
                if (type.FullName != Game.Theme?.GetType().FullName)
                {
                    try
                    {
                        theme = (Theme.Theme)Activator.CreateInstance(type, Game)!;
                    }
                    catch (Exception e)
                    {
                        return $"cannot instantiate theme implementation: {e}";
                    }
                }
            }

            if (gameConfig.ThemeLang is not null && string.IsNullOrWhiteSpace(gameConfig.ThemeLang))
                gameConfig.ThemeLang = "default";
            
            if (gameConfig.AutoFinishVotings == true && gameConfig.UseVotingTimeouts == true)
                return "you cannot have 'auto finish votings' and 'voting timeout' activated at " +
                    "the same time.";
            if (gameConfig.LeaderIsPlayer ?? Game.LeaderIsPlayer)
            {
                if (gameConfig.AutostartVotings == false)
                    return "you cannot change autostart votings if the leader is a player";
                if (gameConfig.UseVotingTimeouts == false)
                    return "you cannot change the voting timeouts if the leader is a player";
                if (gameConfig.AutoFinishVotings == true)
                    return "you cannot change autofinish votings if the leader is a player";
                if (gameConfig.AutoFinishRounds == false)
                    return "you cannot change autofinish rounds if the leader is a player";
            }

            // settings are validated. Now perform changes

            if (gameConfig.Leader is not null && Game.Leader != gameConfig.Leader)
                Game.Leader = gameConfig.Leader;
            if (roleConfig != null)
            {
                Game.RoleConfiguration.Clear();
                foreach (var (k, p) in roleConfig)
                    Game.RoleConfiguration.TryAdd(k, p);
            }
            if (gameConfig.LeaderIsPlayer != null && 
                (Game.LeaderIsPlayer = gameConfig.LeaderIsPlayer.Value))
            {
                Game.AutostartVotings = true;
                Game.UseVotingTimeouts = true;
                Game.AutoFinishVotings = false;
                Game.AutoFinishRounds = true;
            }
            Game.DeadCanSeeAllRoles = gameConfig.DeadCanSeeAllRoles ?? Game.DeadCanSeeAllRoles;
            Game.AllCanSeeRoleOfDead = gameConfig.AllCanSeeRoleOfDead ?? Game.AllCanSeeRoleOfDead;
            Game.AutostartVotings = gameConfig.AutostartVotings ?? Game.AutostartVotings;
            Game.AutoFinishVotings = gameConfig.AutoFinishVotings ?? Game.AutoFinishVotings;
            Game.UseVotingTimeouts = gameConfig.UseVotingTimeouts ?? Game.UseVotingTimeouts;
            Game.AutoFinishRounds = gameConfig.AutoFinishRounds ?? Game.AutoFinishRounds;
            if (theme is not null)
            {
                if (Game.Theme is not null)
                    theme.LanguageTheme = Game.Theme.LanguageTheme;
                Game.Theme = theme;
                foreach (var entry in Game.Users.Values)
                    entry.Role = null;
                Game.RoleConfiguration.Clear();
            }
            if (Game.Theme is not null)
                Game.Theme.LanguageTheme = gameConfig.ThemeLang ?? Game.Theme.LanguageTheme;
            Game.SendEvent(new Theme.Events.SetGameConfig(typeof(Werewolf.Theme.Default.DefaultTheme)));

            return null;
        }
    
        private string? Handle(Events.SetUserConfig userConfig)
        {
            if (userConfig.Theme is not null)
            {
                var check = new Regex("^#[0-9a-fA-F]{6}$");
                if (!check.IsMatch(userConfig.Theme))
                    return "invalid theme color";
            }

            if (!string.IsNullOrWhiteSpace(userConfig.BackgroundImage) &&
                !Uri.TryCreate(userConfig.BackgroundImage, UriKind.Absolute, out _))
                return "invalid background image url";
            
            if (userConfig.Language != null)
            {
                var check = new Regex("^[\\w\\-]{0,10}$");
                if (!check.IsMatch(userConfig.Language))
                    return "invalid language";
            }

            // validation finished
            var config = UserEntry.User.Config;
            config.ThemeColor = userConfig.Theme ?? config.ThemeColor;
            config.BackgroundImage = userConfig.BackgroundImage ?? config.BackgroundImage;
            config.Language = userConfig.Language ?? config.Language;

            UserFactory

            return null;
        }
    }
}