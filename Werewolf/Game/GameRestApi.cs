using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MaxLib.WebServer;
using MaxLib.WebServer.Api.Rest;
using MaxLib.WebServer.Post;
using Werewolf.User;

namespace Werewolf.Game
{
    public class GameRestApi
    {
        class PostRule : ApiRule
        {
            public string Target { get; }

            public string Source { get; }

            public PostRule(string name)
                => Source = Target = name;

            public PostRule(string source, string target)
                => (Source, Target) = (source, target);

            public override bool Check(RestQueryArgs args)
            {
                if (args.Post.Data is UrlEncodedData data && 
                    data.Parameter.TryGetValue(Source, out string? value))
                {
                    args.ParsedArguments[Target] = value;
                    return true;
                }
                else return false;
            }
        }

        public RestApiService BuildService()
        {
            var api = new RestApiService("api");
            var fact = new ApiRuleFactory();
            api.RestEndpoints.AddRange(new[]
            {
                RestActionEndpoint.Create<string>(User, "token")
                    .Add(fact.Location(
                        fact.UrlConstant("user"),
                        fact.MaxLength()
                    ))
                    .Add(new PostRule("token")),
                RestActionEndpoint.Create(LobbyCreate)
                    .Add(fact.Location(
                        fact.UrlConstant("lobby"),
                        fact.UrlConstant("create"),
                        fact.MaxLength()
                    )
                )
            });

            return api;
        }

        private static bool TryParseUserId(string value, out UserId id)
        {
            if (UserId.TryParse(value, out UserId? userId))
            {
                id = userId.Value;
                return true;
            }
            else
            {
                id = default;
                return false;
            }
        }

        private static Func<T, Task<HttpDataSource>> Handler<T>(Func<Utf8JsonWriter, T, Task> action)
        {
            return async x =>
            {
                var stream = new MemoryStream();
                var writer = new Utf8JsonWriter(stream);
                writer.WriteStartObject();

                await action(writer, x);

                writer.WriteEndObject();
                await writer.FlushAsync();
                stream.Position = 0;
                return new HttpStreamDataSource(stream)
                {
                    MimeType = MimeType.ApplicationJson,
                };
            };
        }

        private static Func<T, Task<HttpDataSource>> Handler<T>(Action<Utf8JsonWriter, T> action)
        {
            return async x =>
            {
                var stream = new MemoryStream();
                var writer = new Utf8JsonWriter(stream);
                writer.WriteStartObject();

                action(writer, x);

                writer.WriteEndObject();
                await writer.FlushAsync();
                stream.Position = 0;
                return new HttpStreamDataSource(stream)
                {
                    MimeType = MimeType.ApplicationJson,
                };
            };
        }

        private static Func<T1, T2, Task<HttpDataSource>> Handler<T1, T2>(Func<Utf8JsonWriter, T1, T2, Task> action)
        {
            return async (x1, x2) =>
            {
                var stream = new MemoryStream();
                var writer = new Utf8JsonWriter(stream);
                writer.WriteStartObject();

                await action(writer, x1, x2);

                writer.WriteEndObject();
                await writer.FlushAsync();
                stream.Position = 0;
                return new HttpStreamDataSource(stream)
                {
                    MimeType = MimeType.ApplicationJson,
                };
            };
        }
    
        private static Stream UserToJson(Werewolf.User.UserInfo? user)
        {
            var s = new MemoryStream();
            var w = new Utf8JsonWriter(s);
            try
            {
                if (user is null)
                {
                    w.WriteNullValue();
                    return s;
                }
                w.WriteStartObject();
                w.WriteString("id", user.Id.ToString());
                w.WriteBoolean("is_guest", user.IsGuest);

                w.WriteStartObject("config");
                w.WriteString("username", user.Config.Username);
                w.WriteString("image", user.Config.Image);
                w.WriteString("theme_color", user.Config.ThemeColor);
                w.WriteString("background_image", user.Config.BackgroundImage);
                w.WriteString("language", user.Config.Language);
                w.WriteEndObject();

                w.WriteStartObject("stats");
                w.WriteNumber("win_games", user.Stats.WinGames);
                w.WriteNumber("killed", user.Stats.Killed);
                w.WriteNumber("loose_games", user.Stats.LooseGames);
                w.WriteNumber("leader", user.Stats.Leader);
                w.WriteNumber("level", user.Stats.Level);
                w.WriteNumber("current_xp", user.Stats.CurrentXp);
                w.WriteNumber("level_max_xp", user.Stats.LevelMaxXP);
                w.WriteEndObject();
                
                w.WriteEndObject();

                return s;
            }
            finally
            {
                w.Flush();
            }
        }

        private async Task<HttpDataSource> User(string token)
        {
            var user = GameController.UserFactory is UserController controller ?
                await controller.GetUserFromToken(token).CAF() : null;
            
            return new HttpStreamDataSource(UserToJson(user))
            {
                MimeType = MimeType.ApplicationJson,
            };
        }

        private async Task<HttpDataSource> LobbyCreate()
        {
            await Task.CompletedTask;
            return new HttpStringDataSource("null")
            {
                MimeType = MimeType.ApplicationJson,
            };
        }
    }
}
