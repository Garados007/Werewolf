using System;
using System.Text.Json;

namespace Translate.Report
{
    public class ReportRecord
    {
        /// <summary>
        /// The combined path of the entry
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The original value in the source language.
        /// </summary>
        public string? SourceValue { get; }

        /// <summary>
        /// The translated value to detect user manipulation
        /// </summary>
        public string? TargetValue { get; }

        /// <summary>
        /// The used internal translator. 
        /// </summary>
        public string? Translator { get; }

        /// <summary>
        /// Checks if this was a custom translation. Custom translation will never be updated by
        /// this engine.
        /// </summary>
        public bool IsCustomTranslation 
            => SourceValue is null && Translator is null;

        /// <summary>
        /// Checks if this is a missing translation. Missing translations will always be updated.
        /// </summary>
        public bool IsMissingTranslation
            => SourceValue is not null && Translator is null;

        /// <summary>
        /// Add an automated translated record.
        /// </summary>
        /// <param name="path">the path of the translated value</param>
        /// <param name="value">the source text</param>
        /// <param name="translator">the used translator</param>
        /// <param name="translatedValue">the translated value</param>
        public ReportRecord(Path path, string value, string translator, string translatedValue)
        {
            Path = path.ToString();
            SourceValue = value;
            Translator = translator;
            TargetValue = translatedValue;
        }

        /// <summary>
        /// Add a missing translation.
        /// </summary>
        /// <param name="path">the path of the value</param>
        /// <param name="value">the source text</param>
        public ReportRecord(Path path, string value)
        {
            Path = path.ToString();
            SourceValue = value;
        }

        /// <summary>
        /// Add a custom translation.
        /// </summary>
        /// <param name="path">the path of the value</param>
        public ReportRecord(Path path)
        {
            Path = path.ToString();
        }

        public ReportRecord(JsonElement json)
        {
            Path = json.GetProperty("path").GetString() 
                ?? throw new ArgumentNullException(nameof(json));
            Translator = GetPropertyString(json, "translator");
            SourceValue = GetPropertyString(json, "source-value");
            TargetValue = GetPropertyString(json, "target-value");
        }

        private static string? GetPropertyString(JsonElement node, string key)
        {
            return node.TryGetProperty(key, out JsonElement sub) ? sub.GetString() : null;
        }

        public void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("path", Path);
            if (Translator is null)
                writer.WriteNull("translator");
            else writer.WriteString("translator", Translator);
            if (SourceValue is null)
                writer.WriteNull("source-value");
            else writer.WriteString("source-value", SourceValue);
            if (TargetValue is not null)
                writer.WriteString("target-value", TargetValue);
            writer.WriteEndObject();
        }
    }
}