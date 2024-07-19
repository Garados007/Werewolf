using CommandLine;

namespace LogicCompiler;

internal sealed record Configuration
{
    [Option('s', "source", HelpText = "Source directory with all *.w5logic files")]
    public DirectoryInfo SourceDir { get; set; } = new(Environment.CurrentDirectory);

    [Option('t', "target", HelpText = "Target directory for generated code files. You still need to create a project using dotnet.")]
    public DirectoryInfo TargetDir { get; set; } = new(Environment.CurrentDirectory);

    [Option('n', "namespace", HelpText = "Target namespace to put all files in. Omit this option to use no namespace at all.")]
    public string? NameSpace { get; set; }

    [Option("write-docs", HelpText = "Write docs to the output")]
    public bool WriteDocs { get; set; }

    [Option("write-ast", HelpText = "Write ast to the output")]
    public bool WriteAst { get; set; }

    [Option("test", HelpText = "Special mode for generating test projects")]
    public bool Test { get; set; }
}

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            _ = await Parser.Default.ParseArguments<Configuration>(args)
                .WithParsedAsync(Run);
        }
        catch (AbortException)
        {
            return 1;
        }
        return 0;
    }

    private static async Task RunTest(Configuration config, string @namespace, DirectoryInfo directory)
    {
        if (directory.EnumerateFiles("*.w5logic").Any())
        {
            await Run(config with
            {
                NameSpace = @namespace,
                SourceDir = directory,
                TargetDir = directory,
                Test = false,
            });
            var testName = $"{directory.Name}Test";
            var testFile = $"{testName}.cs";
            if (!directory.EnumerateFiles(testFile).Any())
            {
                await File.WriteAllTextAsync(
                    Path.Combine(directory.FullName, testFile),
                    $$"""
                    using Test.Tools;

                    namespace {{@namespace}};

                    [TestClass]
                    public class {{testName}}
                    {
                        [TestMethod]
                        public async Task TestMethod()
                        {
                            // setup
                            var runner = new Runner<...>();
                            var game = runner.GameRoom;

                            // execute
                            await game.StartGameAsync();
                            IsNotNull(game.Phase);
                        }
                    }
                    """);
            }
        }
        else
        {
            foreach (var dir in directory.EnumerateDirectories())
                await RunTest(config, $"{@namespace}.{dir.Name}", dir);
        }
    }

    private static async Task Run(Configuration config)
    {
        if (config.Test)
        {
            await RunTest(config, config.SourceDir.Name, config.SourceDir);
            return;
        }

        if (!config.SourceDir.Exists)
        {
            Console.Error.WriteLine($"Cannot find source directory: {config.SourceDir}");
            throw new AbortException();
        }
        if (!config.TargetDir.Exists)
        {
            config.TargetDir.Create();
        }

        // parse files
        var files = await ParseAllSourceFiles(config.SourceDir);
        if (files is null)
            throw new AbortException();
        // setup generator
        var generator = new Generator(files);
        if (config.WriteAst)
            await generator.DumpAsync(new FileInfo(Path.Combine(config.TargetDir.FullName, "parsed-data.json")));
        if (config.WriteDocs)
        {
            using var docFuncOutput = new Output(new FileStream(Path.Combine(config.TargetDir.FullName, "Functions.md"), FileMode.OpenOrCreate));
            Functions.Registry.WriteDoc(docFuncOutput);
        }
        using var docOutput = config.WriteDocs ? new Output(new FileStream(Path.Combine(config.TargetDir.FullName, "Node-Methods.md"), FileMode.OpenOrCreate)) : null;
        var validationResult = generator.Validate(docOutput);
        if (config.WriteAst)
            await generator.DumpAsync(new FileInfo(Path.Combine(config.TargetDir.FullName, "validated-data.json")));
        if (!validationResult)
            throw new AbortException();
        // output generated file
        using var output = new Output(new FileStream(Path.Combine(config.TargetDir.FullName, "Logic.cs"), FileMode.OpenOrCreate));
        generator.Write(config, output);
    }

    private static async Task<List<(FileInfo, Grammar.W5LogicParser.ProgramContext)>?> ParseAllSourceFiles(DirectoryInfo source)
    {
        var result = new List<(FileInfo, Grammar.W5LogicParser.ProgramContext)>();
        var hasError = false;
        var awaitBuffer = new List<Task>();
        var workQueue = new Queue<DirectoryInfo>();
        workQueue.Enqueue(source);
        using var mutex = new SemaphoreSlim(1, 1);

        while (workQueue.Count > 0)
        {
            source = workQueue.Dequeue();
            foreach (var file in source.GetFiles("*.w5logic"))
            {
                // indirection binds `file` variable to the call and prevent reassigning through the
                // foreach loop.
                var caller = new Func<FileInfo, Func<Task>>((file) =>
                    new Func<Task>(async () =>
                {
                    var prog = await ParseSourceFile(file);
                    await mutex.WaitAsync();
                    try
                    {
                        if (prog is null)
                            hasError = true;
                        else result.Add((file, prog));
                    }
                    finally { _ = mutex.Release(); }
                }));
                awaitBuffer.Add(Task.Run(caller(file)));
            }
            foreach (var dir in source.GetDirectories())
            {
                workQueue.Enqueue(dir);
            }
        }

        await Task.WhenAll(awaitBuffer);
        return hasError ? null : result;
    }

    private sealed class ErrorListenerL(string fileName) : Antlr4.Runtime.ConsoleErrorListener<int>
    {
        private readonly string fileName = fileName;

        public override void SyntaxError(TextWriter output, Antlr4.Runtime.IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, Antlr4.Runtime.RecognitionException e)
        {
            output.WriteLine($"{fileName}:{line}:{charPositionInLine}: {msg}");
        }
    }

    private sealed class ErrorListenerP(string fileName) : Antlr4.Runtime.ConsoleErrorListener<Antlr4.Runtime.IToken>
    {
        private readonly string fileName = fileName;

        public override void SyntaxError(TextWriter output, Antlr4.Runtime.IRecognizer recognizer, Antlr4.Runtime.IToken offendingSymbol, int line, int charPositionInLine, string msg, Antlr4.Runtime.RecognitionException e)
        {
            output.WriteLine($"{fileName}:{line}:{charPositionInLine}: {msg}");
        }
    }

    private static async Task<Grammar.W5LogicParser.ProgramContext?> ParseSourceFile(FileInfo file)
    {
        try
        {
            var content = await File.ReadAllTextAsync(file.FullName);
            var inputStream = Antlr4.Runtime.CharStreams.fromString(content);
            var lexer = new Grammar.W5LogicLexer(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new ErrorListenerL(file.FullName));
            var tokenStream = new Antlr4.Runtime.BufferedTokenStream(lexer);
            var parser = new Grammar.W5LogicParser(tokenStream);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ErrorListenerP(file.FullName));
            var program = parser.program();
            return parser.NumberOfSyntaxErrors > 0 ? null : program;
        }
        catch (Antlr4.Runtime.RecognitionException e)
        {
            Console.Error.WriteLine($"{file.FullName}: {e.Message}");
            return null;
        }
    }
}
