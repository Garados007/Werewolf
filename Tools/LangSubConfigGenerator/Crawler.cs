using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Werewolf.Theme;
using Werewolf.Theme.Docs;

namespace LangSubConfigGenerator;

public class Crawler
{
    public string ProjectPath { get; }

    private readonly Dictionary<string, string?> phases = new();
    private readonly Dictionary<string, string?> roles = new();
    private readonly Dictionary<string, string?> tags = new();
    private readonly Dictionary<string, (
        string? doc,
        Dictionary<string, (
            string? doc,
            Dictionary<string, string?> vars
        )> options
    )> votings = new();
    private readonly DocumentationLoader docs;

    public Crawler(string projectPath)
    {
        ProjectPath = projectPath;
        docs = new DocumentationLoader(Path.Combine(
            Path.GetDirectoryName(projectPath) ?? ".",
            "bin",
            "Crawler",
            $"{Path.GetFileNameWithoutExtension(projectPath)}.xml"
        ));
    }

    public async Task Run()
    {
        if (!await Compile())
            return;
        var assembly = LoadAssembly();
        if (assembly is null)
            return;
        await docs.Load();
        foreach (var type in assembly.GetTypes())
            CheckType(type);
    }

    private async Task<bool> Compile()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList =
            {
                "build",
                "--nologo",
                "--configuration", "Debug",
                "--output", Path.Combine("bin", "Crawler"),
                "--verbosity", "minimal",
                "/p:GenerateDocumentationFile=true",
                "/nowarn:cs1591"
            },
            WorkingDirectory = Path.GetDirectoryName(ProjectPath),
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        using var process = Process.Start(startInfo);
        if (process is null)
            throw new InvalidOperationException("cannot start dotnet");
        process.ErrorDataReceived += (_, e) => 
        {
            if (e.Data is not null)
                Log.Error("build: {line}", e.Data);
        };
        process.OutputDataReceived += (_, e) => 
        {
            if (e.Data is not null)
                Log.Debug("build: {line}", e.Data);
        };
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        await process.WaitForExitAsync();
        Environment.ExitCode = process.ExitCode;
        return process.ExitCode == 0;
    }

    private Assembly? LoadAssembly()
    {
        var filePath = Path.Combine(
            Path.GetDirectoryName(ProjectPath) ?? ".",
            "bin",
            "Crawler",
            $"{Path.GetFileNameWithoutExtension(ProjectPath)}.dll"
        );
        if (!File.Exists(filePath))
        {
            Log.Error("Cannot find assembly: {path}", filePath);
            Environment.ExitCode = 1;
            return null;
        }
        var assembly = Assembly.LoadFile(Path.GetFullPath(filePath));
        return assembly;
    }

    private void CheckType(Type type)
    {
        Log.Verbose("check {name}", type.FullName);
        CheckPhase(type);
        CheckRole(type);
        CheckVotings(type);

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            Log.Verbose("check method {type}: {name}", type.FullName, method);
            CheckTag(method);
        }
        foreach (var sub in type.GetNestedTypes())
            CheckType(sub);
    }

    private void CheckPhase(Type type)
    {
        foreach (var _ in type.GetCustomAttributes<PhaseAttribute>())
        {
            Phase phase;
            try
            {
                phase = (Phase)(Activator.CreateInstance(type) ?? throw new Exception());
            }
            catch
            {
                Log.Error("error: Cannot load phase {name}", type.FullName);
                return;
            }

            var id = phase.LanguageId;
            Log.Information("found phase {id}", id);
            this.phases[id] = docs.GetDocumentation(type);
        }
    }

    private void CheckRole(Type type)
    {
        foreach (var _ in type.GetCustomAttributes<RoleAttribute>())
        {
            Log.Information("found role {id}", type.Name);
            this.roles[type.Name] = docs.GetDocumentation(type);
        }
    }

    private void CheckTag(MethodInfo method)
    {
        foreach (var attr in method.GetCustomAttributes<TagAttribute>())
        {
            if (this.tags.ContainsKey(attr.Tag))
                continue;
            Log.Information("found tag {id}", attr.Tag);
            this.tags[attr.Tag] = attr.Description;
        }
    }

    private void CheckVotings(Type type)
    {
        var found = false;
        foreach (var _ in type.GetCustomAttributes<VotingAttribute>())
        {
            found = true;
            break;
        }
        if (!found)
            return;
        
        var id = Voting.GetLanguageId(type);

        Log.Information("found voting {id}", id);
        var options = new Dictionary<string, (string?, Dictionary<string, string?>)>();
        votings[id] = (docs.GetDocumentation(type), options);

        SearchVotes(type, options);

        foreach (var attr in type.GetCustomAttributes<VoteVariableAttribute>())
        {
            if (!options.TryGetValue(attr.Vote, out (string?, Dictionary<string, string?> d) result))
                continue;
            result.d[attr.Variable] = attr.Description;
        }
    }

    private void SearchVotes(Type type, Dictionary<string, (string?, Dictionary<string, string?>)> options)
    {
        if (type.BaseType is not null)
            SearchVotes(type.BaseType, options);
        foreach (var vote in type.GetCustomAttributes<VoteAttribute>())
        {
            if (vote.Remove)
                options.Remove(vote.Vote);
            else
            {
                options[vote.Vote] = (vote.Description, new Dictionary<string, string?>());
            }
        }
    }

    public async Task Export(string path)
    {
        Log.Debug("exporting ...");
        using var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        using var w = new Utf8JsonWriter(file, new JsonWriterOptions
        {
            Indented = true,
        });
        w.WriteStartObject();
        w.WriteString("$schema", "./schema.json");

        w.WriteStartObject("Phase");
        foreach (var (phase, doc) in phases)
        {
            w.WriteStartObject(phase);
            w.WriteString("description", doc ?? "");
            w.WriteEndObject();
        }
        w.WriteEndObject(); // Phase

        w.WriteStartObject("Role");
        foreach (var (role, doc) in roles)
        {
            w.WriteStartObject(role);
            w.WriteString("description", doc ?? "");
            w.WriteEndObject();
        }
        w.WriteEndObject(); // Role

        w.WriteStartObject("Tag");
        foreach (var (tag, doc) in tags)
        {
            w.WriteStartObject(tag);
            w.WriteString("description", doc ?? "");
            w.WriteEndObject();
        }
        w.WriteEndObject(); // Tag

        w.WriteStartObject("Voting");
        w.WriteStartObject("default");
        w.WriteString("description", "the default value for all votings");
        w.WriteEndObject();
        w.WriteStartObject("default-logs");
        w.WriteString("description", "the default value for all votings if the voting is logged");
        w.WriteEndObject();
        foreach (var (voting, (doc, _)) in votings)
        {
            w.WriteStartObject(voting);
            w.WriteString("description", doc ?? "");
            w.WriteStartArray("fallback");
            w.WriteStringValue("default");
            w.WriteEndArray(); // fallback
            w.WriteEndObject();
        }
        w.WriteEndObject();

        w.WriteStartObject("VotingWithVotes");
        w.WriteStartObject("default");
        w.WriteString("description", "the default text of the options in all votings");
        w.WriteStartObject("nodes");
        var set = new HashSet<string>();
        foreach (var (_, (doc, options)) in votings)
        {
            foreach (var (option, (doc2, vars)) in options)
            {
                if (!set.Add(option))
                    continue;
                w.WriteStartObject(option);
                w.WriteString("description", doc2 ?? "");
                if (vars.Count > 0)
                {
                    w.WriteStartObject("variables");
                    foreach (var (key, doc3) in vars)
                    {
                        w.WriteString(key, doc3 ?? "");
                    }
                    w.WriteEndObject(); // variables
                }
                w.WriteEndObject();
            }
        }
        w.WriteEndObject(); // nodes
        w.WriteEndObject(); // default
        foreach (var (voting, (doc, options)) in votings)
        {
            w.WriteStartObject(voting);
            w.WriteString("description", doc ?? "");
            w.WriteStartObject("nodes");
            foreach (var (option, (doc2, vars)) in options)
            {
                w.WriteStartObject(option);
                w.WriteString("description", doc2 ?? "");
                if (vars.Count > 0)
                {
                    w.WriteStartObject("variables");
                    foreach (var (key, doc3) in vars)
                    {
                        w.WriteString(key, doc3 ?? "");
                    }
                    w.WriteEndObject(); // variables
                }
                w.WriteStartArray("fallback");
                w.WriteStringValue("default");
                w.WriteStringValue(option);
                w.WriteEndArray(); // fallback
                w.WriteEndObject();
            }
            w.WriteEndObject(); // nodes
            w.WriteEndObject();
        }
        w.WriteEndObject();

        w.WriteEndObject();
        await w.FlushAsync();
        file.SetLength(file.Position);
        Log.Debug("export finished");
    }
}
