using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace Translate
{
    public class DeepL
    {
        public string AuthKey { get; }

        private readonly WebClient client;

        public DeepL(string authKey)
        {
            AuthKey = authKey;
            client = new WebClient();
            client.BaseAddress = "https://api-free.deepl.com/";
            client.Encoding = System.Text.Encoding.UTF8;
        }

        public async Task<(long max, long current)?> GetLimitsAsync()
        {
            client.Headers.Set("Authorization", $"DeepL-Auth-Key: {AuthKey}");
            byte[] response;
            try
            {
                response = await client.DownloadDataTaskAsync("/v2/usage")
                    .ConfigureAwait(false);
            }
            catch (WebException e)
            {
                Log.Error(e, "Cannot get limits");
                return null;
            }
            var d = JsonDocument.Parse(response);
            if (!d.RootElement.TryGetProperty("character_count", out JsonElement node)
                || !node.TryGetInt64(out long max))
                return null;
            if (!d.RootElement.TryGetProperty("character_limit", out node)
                || !node.TryGetInt64(out long current))
                return null;
            return (max, current);
        }

        public async Task<string?> GetTranslationAsync(string source, string target, string text)
        {
            client.Headers.Set("Authorization", $"DeepL-Auth-Key: {AuthKey}");
            byte[] response;
            var col = new System.Collections.Specialized.NameValueCollection();
            col.Set("text", text);
            col.Set("source_lang", source);
            col.Set("target_lang", target);
            col.Set("formality", "less");
            try
            {
                response = await client.UploadValuesTaskAsync("/v2/translate", col)
                    .ConfigureAwait(false);
            }
            catch (WebException e)
            {
                Log.Error(e, "Cannot get translation");
                return null;
            }
            var d = JsonDocument.Parse(response);
            if (!d.RootElement.TryGetProperty("translations", out JsonElement node)
                || node.ValueKind != JsonValueKind.Array
                || node.GetArrayLength() < 1
                || !node[0].TryGetProperty("text", out node))
                return null;
            return node.GetString();
        }
    }
}