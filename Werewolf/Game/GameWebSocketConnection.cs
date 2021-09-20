using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MaxLib.WebServer.WebSocket;
using Werewolf.Theme;
using Werewolf.User;

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
                if (GameController.Current.RemoveWsConnection(this))
                    entry.RemoveConnection();
                game.SendEvent(new Theme.Events.OnlineNotification(entry));
            };
            _ = SendFrame(new Events.SendGameData(
                game, 
                entry.User,
                userFactory
            ));
            game.SendEvent(new Theme.Events.OnlineNotification(entry));
        }

        protected override Task ReceiveClose(CloseReason? reason, string? info)
        {
            if (GameController.Current.RemoveWsConnection(this))
                UserEntry.RemoveConnection();
            Game.SendEvent(new Theme.Events.OnlineNotification(UserEntry));
            return Task.CompletedTask;
        }

        public async Task SendEvent(GameEvent @event)
        {
            await SendFrame(new Events.SubmitGameEvents(@event, Game, UserEntry.User)).CAF();
        }

        public async Task SendEvent(EventBase @event)
            => await SendFrame(@event).CAF();

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
                        await SendResult(
                            setGameConfig, 
                            Handle(setGameConfig), 
                            false
                        ).CAF();
                        break;
                    case Events.SetUserConfig setUserConfig:
                        await SendResult(
                            setUserConfig, 
                            await Handle(setUserConfig).CAF(),
                            false
                        ).CAF();
                        break;
                    case Events.GameStart gameStart:
                        await SendResult(
                            gameStart,
                            await Handle(gameStart).CAF(),
                            false
                        ).CAF();
                        break;
                    case Events.GameNext gameNext:
                        await SendResult(
                            gameNext,
                            await Handle(gameNext).CAF(),
                            false
                        ).CAF();
                        break;
                    case Events.GameStop gameStop:
                        await SendResult(
                            gameStop,
                            await Handle(gameStop).CAF(),
                            false
                        ).CAF();
                        break;
                    case Events.VotingStart votingStart:
                        await SendResult(
                            votingStart,
                            Handle(votingStart),
                            false
                        ).CAF();
                        break;
                    case Events.Vote vote:
                        await SendResult(
                            vote,
                            Handle(vote),
                            false
                        ).CAF();
                        break;
                    case Events.VotingWait votingWait:
                        await SendResult(
                            votingWait,
                            Handle(votingWait),
                            false
                        ).CAF();
                        break;
                    case Events.VotingFinish votingFinish:
                        await SendResult(
                            votingFinish,
                            await Handle(votingFinish).CAF(),
                            false
                        ).CAF();
                        break;
                    case Events.KickUser kickUser:
                        await SendResult(
                            kickUser,
                            Handle(kickUser),
                            false
                        ).CAF();
                        break;
                    case Events.Message message:
                        await SendResult(
                            message,
                            Handle(message),
                            false
                        ).CAF();
                        break;
                    case Events.RefetchJoinToken refetchJoinToken:
                        await SendResult(
                            refetchJoinToken,
                            await Handle(refetchJoinToken).CAF(),
                            false
                        ).CAF();
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
                
            if (gameConfig.Leader is not null && !Game.Users.ContainsKey(gameConfig.Leader.Value))
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
                Game.Leader = gameConfig.Leader.Value;
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
    
        private async Task<string?> Handle(Events.SetUserConfig userConfig)
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
            await Task.WhenAll(new []
            {
                config.SetThemeColorAsync(userConfig.Theme ?? config.ThemeColor),
                config.SetBackgroundImageAsync(userConfig.BackgroundImage ?? config.BackgroundImage),
                config.SetLanguageAsync(userConfig.Language ?? config.Language),
            }.Select(x => x.AsTask())).CAF();

            var task = Game.Theme?.Users.GetUser(UserEntry.User.Id, false).CAF();
            if (task.HasValue)
                await task.Value;
            Game.SendEvent(new Theme.Events.SetUserConfig(UserEntry.User));

            return null;
        }

        private async Task<string?> Handle(Events.GameStart gameStart)
        {
            if (UserEntry.User.Id != Game.Leader)
                return "you are not the leader of the group";

            if (Game.Phase != null)
                return "game is already running";

            if (!Game.FullConfiguration)
                return "some roles are missing or there are to much roles defined";

            if (Program.MaintenanceMode)
                return "Server is in maintenance mode. You cannot create a new game. Try to create a new lobby.";

            var random = new Random();
            var roles = Game.RoleConfiguration
                .SelectMany(x => Enumerable.Repeat(x.Key, x.Value))
                .Select(x => x.CreateNew())
                .ToList();
            var players = Game.Users.Values
                .Where(x => Game.LeaderIsPlayer || x.User.Id != UserEntry.User.Id)
                .ToArray();

            foreach (var player in players)
            {
                var index = random.Next(roles.Count);
                player.Role = roles[index];
                roles.RemoveAt(index);
            }

            await Game.StartGameAsync().CAF();
            return null;
        }

        private async Task<string?> Handle(Events.GameNext gameNext)
        {
            if (UserEntry.User.Id != Game.Leader)
                return "you are not the leader of the group";

            if (Game.Phase == null)
                return "there is no current phase";

            await Game.NextPhaseAsync().CAF();

            return null;
        }

        private async Task<string?> Handle(Events.GameStop gameStop)
        {
            if (UserEntry.User.Id != Game.Leader)
                return "you are not the leader of the group";

            if (Game.Phase == null)
                return "the game is not running";

            await Game.StopGameAsync(null).CAF();

            return null;
        }

        private string? Handle(Events.VotingStart votingStart)
        {
            if (UserEntry.User.Id != Game.Leader)
                return "you are not the leader of the group";

            if (Game.LeaderIsPlayer)
                return "as a player you cannot start a voting";

            var voting = Game.Phase?.Current.Votings
                .Where(x => x.Id == votingStart.VotingId)
                .FirstOrDefault();

            if (voting == null)
                return "no voting exists";

            if (voting.Started)
                return "voting already started";

            voting.Started = true;
            if (Game.UseVotingTimeouts)
                voting.SetTimeout(Game, true);

            return null;
        }

        private string? Handle(Events.Vote vote)
        {
            var voting = Game.Phase?.Current.Votings
                .Where(x => x.Id == vote.VotingId)
                .FirstOrDefault();
            if (voting == null)
                return "no voting exists";

            if (!Game.Users.TryGetValue(UserEntry.User.Id, out GameUserEntry? entry))
                entry = null;
            var ownRole = entry?.Role;
            if (ownRole == null || !voting.CanVote(ownRole))
                return "you are not allowed to vote";

            if (!voting.Started)
                return "voting is not started";
            
            return voting.Vote(Game, UserEntry.User.Id, vote.EntryId);
        }

        private string? Handle(Events.VotingWait votingWait)
        {
            var voting = Game.Phase?.Current.Votings
                .Where(x => x.Id == votingWait.VotingId)
                .FirstOrDefault();
            if (voting == null)
                return "no voting exists";

            if (!Game.Users.TryGetValue(UserEntry.User.Id, out GameUserEntry? entry))
                entry = null;
            var ownRole = entry?.Role;
            if ((UserEntry.User.Id != Game.Leader || Game.LeaderIsPlayer) 
                && (ownRole == null || !voting.CanVote(ownRole)))
                return "you are not allowed to vote";

            if (!voting.Started)
                return "voting is not started";

            if (!Game.UseVotingTimeouts)
                return "there are no timeouts activated for this voting";

            if (!voting.SetTimeout(Game, false))
                return "timeout already reseted. Try later!";

            return null;
        }

        private async Task<string?> Handle(Events.VotingFinish votingFinish)
        {
            if (UserEntry.User.Id != Game.Leader)
                return "you are not the leader of the group";

            if (Game.LeaderIsPlayer)
                return "as a player you cannot finish a voting";

            var voting = Game.Phase?.Current.Votings
                .Where(x => x.Id == votingFinish.VotingId)
                .FirstOrDefault();
            if (voting == null)
                return "no voting exists";

            if (!voting.Started)
                return "voting is not started";

            await voting.FinishVotingAsync(Game).CAF();

            return null;
        }

        private string? Handle(Events.KickUser kickUser)
        {
            if (UserEntry.User.Id != Game.Leader)
                return "you are not the leader of the group";

            if (!Game.Users.TryGetValue(kickUser.User, out GameUserEntry? entry)
                || entry.Role is null)
                return "player is not a participant";

            Game.RemoveParticipant(entry.User);
            Game.SendEvent(new Theme.Events.RemoveParticipant(kickUser.User));

            return null;
        }

        private string? Handle(Events.Message message)
        {
            var currentPhase = Game.Phase?.Current;
            var current = currentPhase?.LanguageId;
            var role = Game.TryGetRoleKindSafe(UserEntry.User.Id);
            var allowed = role.IsLeader ||
                currentPhase == null ||
                (current == message.Phase && role != null && currentPhase.CanMessage(Game, role));
            Game.SendEvent(new Theme.Events.ChatEvent(
                UserEntry.User.Id, 
                message.Phase, 
                message.Content, 
                allowed
            ));

            return null;
        }

        private async Task<string?> Handle(Events.RefetchJoinToken refetchJoinToken)
        {

            if (UserEntry.User.Id != Game.Leader)
                return "you are not the leader of the group";

            if (Program.MaintenanceMode)
                return "This server is in maintenance mode. You cannot create a new join token.";

            var joinToken = await GameController.Current.GetJoinTokenAsync(Game.Id).CAF();

            if (joinToken is null)
                return "no join token available";

            _ = this.SendFrame(new Events.GetJoinToken()
            {
                AliveUntil = joinToken.AliveUntil,
                Token = joinToken.Token,
            }).CAF();

            return null;
        }
    }
}