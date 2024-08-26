using System.Web;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Translate.Report
{
    public class ReportGenerator : IDisposable
    {
        private class Info
        {
            public int Missing, Custom;
            public readonly Dictionary<string, int> Translator = new Dictionary<string, int>();
        }

        private readonly StreamWriter w;
        
        private readonly string systemPath;

        public ReportGenerator(string file, string title, string systemPath)
        {
            var dir = System.IO.Path.GetDirectoryName(file);
            if (!Directory.Exists(dir) && dir is not null)
                Directory.CreateDirectory(dir);
            this.systemPath = new DirectoryInfo(systemPath).FullName;
            w = new StreamWriter(
                new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite),
                System.Text.Encoding.UTF8
            );
            WriteHeader(title);
        }

        private string? TrimPath(string? path)
        {
            if (path is null)
                return null;
            if (path.StartsWith(systemPath))
                return path.Substring(systemPath.Length);
            return path;
        }

        private void WriteHeader(string title)
        {
            w.WriteLine("<!DOCTYPE html>");
            w.Write($"<html><head><title>{HttpUtility.HtmlEncode(title)}</title><meta charset=\"utf-8\"/>");
            w.Write("<script>function toggle(id,mod){let item=document.getElementById(id);item." +
                "style.display=item.style.display=='none'?mod:'none';document.getElementById(" +
                "'toggle-'+id).classList.toggle('open')}function group_open(id){document." +
                "querySelectorAll('tr.'+id).forEach(x=>x.style.display='table-row');document." +
                "querySelectorAll('div.'+id).forEach(x=>x.classList.add('open'))}function " +
                "group_close(id){document.querySelectorAll('tr.'+id).forEach(x=>x.style.display=" +
                "'none');document.querySelectorAll('div.'+id).forEach(x=>x.classList.remove(" +
                "'open'))}</script>");
            w.Write("<link rel=\"stylesheet\" href=\"style.css\"/>");
            w.Write($"</head><body><h1>{HttpUtility.HtmlEncode(title)}</h1>");
            w.Flush();
        }

        int reportLineIndex;
        int reportGroupIndex;
        readonly Dictionary<string, Info> reportInfos = new Dictionary<string, Info>();

        public void AddReport(ReportStatus report, string path)
        {
            if (!reportInfos.TryGetValue(path, out Info? info))
                reportInfos.Add(path, info = new Info());
            reportGroupIndex++;
            w.Write($"<h2>{HttpUtility.HtmlEncode(TrimPath(path))}</h2>" +
                $"<p class=\"actions\"><span onclick=\"group_open('group-{reportGroupIndex}')\">" +
                $"open all</span> <span onclick=\"group_close('group-{reportGroupIndex}')\">close" +
                $" all</span></p><table class=\"report\"><thead><tr><th></th><th>Key</th><th>" +
                $"Translator</th></tr></thead><tbody>"
            );
            int localLine = 0;
            foreach (var entry in report)
            {
                localLine++;
                if (localLine % 2 == 0)
                    w.Write($"<tr class=\"even\"><td>");
                else w.Write($"<tr><td>");
                if (entry.SourceValue is not null)
                {
                    w.Write($"<div onclick=\"toggle('line-{++reportLineIndex}','table-row')\" " +
                        $"id=\"toggle-line-{reportLineIndex}\" class=\"group-{reportGroupIndex}\""+
                        $"></div>"
                    );
                }
                w.Write($"</td><td>{HttpUtility.HtmlEncode(TrimPath(entry.Path))}</td><td>");
                if (entry.IsCustomTranslation)
                    w.Write("<span class=\"custom\">custom</span>");
                else if (entry.Translator is not null)
                {
                    w.Write($"<span class=\"{HttpUtility.HtmlAttributeEncode(entry.Translator)}\"" +
                        $">{HttpUtility.HtmlEncode(entry.Translator)}</span>");
                }
                w.Write($"</td></tr>");
                if (entry.SourceValue is not null)
                {
                    w.Write(
                        $"<tr id=\"line-{reportLineIndex}\" class=\"group-{reportGroupIndex}" +
                        $"{(localLine % 2 == 0 ? " even" : "")}\" style=\"display:none\">" +
                        $"<td></td><td colspan=\"2\"><table class=\"report-info\"><tr><th>Source:" +
                        $"</th><td>{HttpUtility.HtmlEncode(entry.SourceValue)}</td></tr>"
                    );
                    if (entry.TargetValue is not null)
                        w.Write($"<tr><th>Target</th><td>{HttpUtility.HtmlEncode(entry.TargetValue)}</td></tr>");
                    w.Write($"</table></td></tr>");
                }

                if (entry.IsCustomTranslation)
                    info.Custom++;
                if (entry.IsMissingTranslation)
                    info.Missing++;
                if (entry.Translator is not null)
                    info.Translator[entry.Translator] = 
                        info.Translator.TryGetValue(entry.Translator, out int old) ? old + 1 : 1;
            }
            w.Write($"</tbody></table>");
            w.Flush();
        }

        public void WriteFinalReport(Priority priority)
        {
            w.Write("<div class=\"results\"><h2>Results</h2><table class=\"result\"><thead><tr>");
            w.Write($"<th>File</th><th>Missing</th><th>Custom</th>");
            var translators = reportInfos.Values
                .SelectMany(x => x.Translator.Keys)
                .Distinct()
                .OrderBy(x => priority.GetPriority(x))
                .ToArray();
            foreach (var translator in translators)
                w.Write($"<th>{HttpUtility.HtmlEncode(translator)}</th>");
            w.Write($"</tr></thead><tbody>");
            int localLine = 0;
            var total = new Info();
            foreach (var (path, info) in reportInfos)
            {
                localLine++;
                w.Write($"<tr{(localLine % 2 == 0 ? " class=\"event\"" : "")}><td>" +
                    $"{HttpUtility.HtmlEncode(TrimPath(path))}</td><td>{info.Missing}</td>" +
                    $"<td>{info.Custom}</td>");
                foreach (var translator in translators)
                {
                    w.Write("<td>");
                    if (info.Translator.TryGetValue(translator, out int count))
                        w.Write(count);
                    w.Write($"</td>");
                }
                w.Write("</tr>");
                total.Custom += info.Custom;
                total.Missing += info.Missing;
                foreach (var (translator, count) in info.Translator)
                {
                    total.Translator[translator] = 
                        total.Translator.TryGetValue(translator, out int old) ? old + count : count;
                }
            }
            var all = total.Custom + total.Missing + total.Translator.Values.Sum();
            w.Write("</tbody><tfoot><tr><td>Sum</td><td>");
            WriteSum(total.Missing, all);
            w.Write("</td><td>");
            WriteSum(total.Custom, all);
            foreach (var translator in translators)
            {
                w.Write("</td><td>");
                if (total.Translator.TryGetValue(translator, out int count))
                    WriteSum(count, all);
            }
            w.Write("</tr></tfoot></table></div>");
        }

        private void WriteSum(int sum, int total)
        {
            w.Write($"{sum:#,##0} ({((float)sum/total):#0.00%})");
        }

        public void Dispose()
        {
            w.Flush();
            w.BaseStream.SetLength(w.BaseStream.Position);
            ((IDisposable)w).Dispose();
        }
    }
}