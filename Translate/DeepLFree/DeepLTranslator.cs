using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;

namespace Translate.DeepLFree
{
    public class DeepLTranslator : ITranslator
    {
        public string Key => "deepl-free";

        public bool Wait { get; }

        public DeepLTranslator(bool wait)
        {
            Wait = wait;
        }

        private int errorCounter;
        /// <summary>
        /// if <see cref="errorCounter" /> reaches this limit this translator will no longer be
        /// available.
        /// </summary>
        private const int ErrorLimit = 5;

        public bool CanTranslate(string value)
        {
            return errorCounter < ErrorLimit;
        }

        public Task<(long max, long current)?> GetLimitsAsync()
        {
            return Task.FromResult<(long, long)?>(null);
        }

        static Regex filterNonError = new Regex("Using .* server backend.", RegexOptions.Compiled);

        public async Task<string?> GetTranslationAsync(string source, string target, string text)
        {
            if (errorCounter >= ErrorLimit)
                return null;
            
            if (Wait)
            {
                // wait for 1 to 5 minutes
                var r = new Random();
                var time = TimeSpan.FromMilliseconds(r.Next(60_000, 300_000));
                Serilog.Log.Debug("Wait for {time}", time);
                await Task.Delay(time);
            }
            
            var startInfo = new ProcessStartInfo
            {
                ArgumentList =
                {
                    "DeepLFree/translator.py",
                    text,
                    source,
                    target
                },
                FileName = "python",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
            };
            using var process = Process.Start(startInfo);
            if (process is null)
                return null;
            await process.WaitForExitAsync().ConfigureAwait(false);
            // check for errors
            var sb = new StringBuilder();
            string? line;
            while ((line = await process.StandardError.ReadLineAsync().ConfigureAwait(false)) is not null)
            {
                if (filterNonError.IsMatch(line) || string.IsNullOrWhiteSpace(line))
                    continue;
                if (line.Contains("429 Client Error: Too many Requests"))
                {
                    errorCounter = ErrorLimit;
                    return null;
                }
                sb.AppendLine(line);
            }
            if (sb.Length > 0)
            {
                Serilog.Log.Error("DeepL: translation error {err}", sb);
                errorCounter++;
                return null;
            }
            // the plain output is what we want
            var result = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            if (result is not null)
                errorCounter = 0;
            return Verify(text, result);
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