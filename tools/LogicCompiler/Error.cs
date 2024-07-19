namespace LogicCompiler;

internal static class Error
{
    private enum Level
    {
        info = 0,
        warn = 1,
        error = 2,
        fatal = 3,
    }

    private static readonly SemaphoreSlim mutex = new(1, 1);

    public static bool HasError { get; private set; }

    public static void WriteWarning(Ast.ISourceNode source, string msg)
    => Write(Level.warn, source.SourceFile, source.SourceToken, msg);

    public static void WriteWarning(FileInfo? source, Antlr4.Runtime.IToken? start, string msg)
    => Write(Level.warn, source, start, msg);

    public static void WriteError(Ast.ISourceNode source, string msg)
    => Write(Level.error, source.SourceFile, source.SourceToken, msg);

    public static void WriteError(FileInfo? source, Antlr4.Runtime.IToken? start, string msg)
    => Write(Level.error, source, start, msg);

    public static AbortException WriteFatal(FileInfo? source, Antlr4.Runtime.IToken? start, string msg)
    {
        Write(Level.fatal, source, start, msg);
        return new AbortException();
    }

    public static AbortException WriteFatal(Ast.ISourceNode source, string msg)
    {
        Write(Level.fatal, source.SourceFile, source.SourceToken, msg);
        return new AbortException();
    }


    private static void Write(Level level, FileInfo? source, Antlr4.Runtime.IToken? start, string msg)
    {
        mutex.Wait();
        if ((int)level >= (int)Level.error)
            HasError = true;
        try
        {
            if (start is null)
            {
                Console.Error.WriteLine($"{source?.FullName ?? "<unknown"}: {level}: {msg}");
            }
            else
            {
                Console.Error.WriteLine($"{source?.FullName ?? "<unknown"}({start.Line},{start.Column + 1}): {level}: {msg}");
            }
        }
        finally
        {
            _ = mutex.Release();
        }
    }
}
