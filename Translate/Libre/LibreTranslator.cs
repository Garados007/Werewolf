using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibreTranslate.Net;

namespace Translate.Libre
{
    public class LibreTranslator : ITranslator
    {
        public string Key => "libre";

        private readonly LibreTranslate.Net.LibreTranslate Translator;

        public LibreTranslator()
        {
            Translator = new LibreTranslate.Net.LibreTranslate(
                "https://libretranslate.de/"
            );
        }

        public bool CanTranslate(string value)
        {
            return true;
        }

        public Task<(long max, long current)?> GetLimitsAsync()
        {
            return Task.FromResult<(long, long)?>(null);
        }

        public async Task<string?> GetTranslationAsync(string source, string target, string text)
        {
            var sourceCode = GetCode(source);
            var targetCode = GetCode(target);
            if (sourceCode is null || targetCode is null)
                return null;
            try
            {
                return Verify(text, await Translator.TranslateAsync(new LibreTranslate.Net.Translate
                {
                    ApiKey = "",
                    Source = sourceCode,
                    Target = targetCode,
                    Text = text
                }).ConfigureAwait(false));
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, "Libre: cannot translate {source}->{target}", source, target);
                return null;
            }
        }

        private LanguageCode? GetCode(string lang)
        {
            switch (lang)
            {
                case "de": return LanguageCode.German;
                case "en": return LanguageCode.English;
                default: return null;
            }
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