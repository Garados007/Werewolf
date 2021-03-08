using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MaxLib.WebServer;
using MaxLib.WebServer.Api.Rest;
using MaxLib.WebServer.Post;
using Werewolf.Users.Api;

namespace Werewolf.Game
{
    public class GameRestApi
    {
        class PostRule : ApiRule
        {
            public string Target { get; }

            public PostRule(string target)
                => Target = target;

            public override bool Check(RestQueryArgs args)
            {
                if (args.Post.Data is UrlEncodedData data)
                {
                    args.ParsedArguments[Target] = data;
                    return true;
                }
                else return false;
            }
        }

        public RestApiService BuildService()
        {
            var api = new RestApiService("api");
            var fact = new ApiRuleFactory();


            return api;
        }

        private static bool TryParseUserId(string value, out UserId id)
        {
            id = new UserId();
            try { id.FromId(value); }
            catch { return false; }
            return true;
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
    }
}
