using System.Threading.Tasks;

namespace Translate
{
    public interface ITranslator
    {
        string Key { get; }

        bool CanTranslate(string value);

        Task<(long max, long current)?> GetLimitsAsync();

        Task<string?> GetTranslationAsync(string source, string target, string text);
    }
}
