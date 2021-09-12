using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using System.Collections;

namespace Translate.Report
{
    public class ReportStatus : IEnumerable<ReportRecord>
    {
        private readonly Dictionary<string, ReportRecord> records
            = new Dictionary<string, ReportRecord>();

        public ReportStatus() {}

        public ReportStatus(string file)
        {
            if (!File.Exists(file))
                return;
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var doc = JsonDocument.Parse(stream);
            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                var record = new ReportRecord(entry);
                records.Add(record.Path, record);
            }
        }

        public ReportRecord? Get(Path path)
        {
            if (records.TryGetValue(path.ToString(), out ReportRecord? value))
                return value;
            else return null;
        }

        public void Add(ReportRecord record)
        {
            records[record.Path] = record;
        }

        public async Task Save(string file)
        {
            var dir = System.IO.Path.GetDirectoryName(file);
            if (dir is not null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            using var stream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                // this optimizes git diffs
                Indented = true,
            });
            writer.WriteStartArray();
            foreach (var entry in records)
                entry.Value.Write(writer);
            writer.WriteEndArray();
            await writer.FlushAsync().ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
            stream.SetLength(stream.Position);
        }

        public IEnumerator<ReportRecord> GetEnumerator()
            => records.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}