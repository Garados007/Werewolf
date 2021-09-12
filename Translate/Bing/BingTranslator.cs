using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Translate.Bing
{
    public class BingTranslator : ITranslator
    {
        public string Key => "bing";

        private int errorCounter;
        /// <summary>
        /// if <see cref="errorCounter" /> reaches this limit this translator will no longer be
        /// available. This is required because bing.com is rate limited.
        /// </summary>
        private const int ErrorLimit = 5;

        public bool CanTranslate(string value)
        {
            return value.Length <= 1000 && value.Length > 0 && errorCounter < ErrorLimit;
        }

        public Task<(long max, long current)?> GetLimitsAsync()
        {
            return Task.FromResult<(long, long)?>(null);
        }

        public async Task<string?> GetTranslationAsync(string source, string target, string text)
        {
            if (errorCounter >= ErrorLimit)
                return null;
            // // only for debug
            // if (source == "de" && target == "en" && text == "Nutzer zurücksetzen")
            //     return "reset user";
            // if (source == "de" && target == "en" && text == "Mit Account spielen")
            //     return "play with Account";
            // return null;
            // #pragma warning disable CS0162
            var startInfo = new ProcessStartInfo
            // #pragma warning restore CS0162
            {
                ArgumentList =
                    {
                        "Bing/translator.js",
                        text,
                        source,
                        target
                    },
                FileName = "node",
                RedirectStandardOutput = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
            };
            using var process = Process.Start(startInfo);
            if (process is null)
                return null;
            await process.WaitForExitAsync().ConfigureAwait(false);
            var result = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            var json = JsonDocument.Parse(result);
            if (json.RootElement.TryGetProperty("result", out JsonElement node))
            {
                errorCounter = 0;
                return Verify(text, node.GetString());
            }
            if (json.RootElement.TryGetProperty("err", out node))
            {
                Serilog.Log.Error("Bing: translation error {err}", node);
                errorCounter++;
            }
            return null;
        }

        static Regex matcher = new Regex("(\\{[^\\}]+\\})", RegexOptions.Compiled);

        private static string? Verify(string source, string? target)
        {
            if (target is null || !source.Contains('{'))
                return target;
            foreach (Match match in matcher.Matches(source))
            {
                if (!match.Success)
                    continue;
                foreach (Capture capture in match.Groups[1].Captures)
                    if (!target.Contains(capture.Value))
                        return null;
            }
            return target;
        }
    }
}