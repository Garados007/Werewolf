using LogicCompiler.Grammar;
using LogicCompiler.Ast;
using LogicTools;

namespace LogicCompiler;

internal sealed class Generator
{
    public Dictionary<string, ModeNode> Modes { get; } = [];
    public Dictionary<string, PhaseNode> Phases { get; } = [];
    public Dictionary<string, SceneNode> Scenes { get; } = [];
    public Dictionary<string, LabelNode> Labels { get; } = [];
    public Dictionary<string, CharacterNode> Characters { get; } = [];
    public Dictionary<string, VotingNode> Votings { get; } = [];
    public Dictionary<string, OptionNode> Options { get; } = [];
    public Dictionary<string, WinNode> Wins { get; } = [];
    public Dictionary<string, SequenceNode> Sequences { get; } = [];
    public Dictionary<string, EventNode> Events { get; } = [];

    public Flow? Flow { get; set; }

    public Generator(List<(FileInfo, W5LogicParser.ProgramContext)> progs)
    {
        foreach (var (source, ctx) in progs)
        {
            var file = new Ast.File(source, ctx);
            Move(source, Modes, file.Modes);
            Move(source, Phases, file.Phases);
            Move(source, Scenes, file.Scenes);
            Move(source, Labels, file.Labels);
            Move(source, Characters, file.Characters);
            Move(source, Votings, file.Votings);
            Move(source, Options, file.Options);
            Move(source, Wins, file.Wins);
            Move(source, Sequences, file.Sequences);
            Move(source, Events, file.Events);
        }
    }

    private static void Move<T>(FileInfo sourceFile, Dictionary<string, T> target, List<T> source)
        where T : NodeBase
    {
        foreach (var item in source)
        {
            var name = item.Name.Text;
            if (!target.TryAdd(name, item))
            {
                Error.WriteError(sourceFile, item.Source?.Start, $"Redefinition of {typeof(T).Name} `{name}`.");
            }
        }
    }

    public bool Validate(Output? docs)
    {
        return new Validator(this, docs).Validate();
    }

    private static readonly System.Text.Json.JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        },
        // TypeInfoResolver = new PolymorphicTypeResolver(),
    };

    public async Task DumpAsync(FileInfo target)
    {
        if ((!target.Directory?.Exists) ?? false)
        {
            target.Directory!.Create();
        }
        using var file = new FileStream(target.FullName, FileMode.OpenOrCreate);
        await System.Text.Json.JsonSerializer.SerializeAsync(file, this, jsonSerializerOptions);
        await file.FlushAsync();
        file.SetLength(file.Position);
    }

    public async Task DumpInfo(FileInfo target)
    {
        if ((!target.Directory?.Exists) ?? false)
        {
            target.Directory!.Create();
        }
        using var file = new FileStream(target.FullName, FileMode.OpenOrCreate);
        await System.Text.Json.JsonSerializer.SerializeAsync(file, GetInfo(), jsonSerializerOptions);
        await file.FlushAsync();
        file.SetLength(file.Position);
    }

    public Info GetInfo()
    {
        static IEnumerable<string> SearchForUsedOptions(CodeBlock block)
        {
            return ((IAstQueryable)block)
                .GetAll(x => x is TypedNameExpression)
                .Cast<TypedNameExpression>()
                .Where(x => x.Type == NameType.Option)
                .Select(x => x.Name.Text)
                .Distinct();
        }
        return new Info
        {
            Modes = { Modes.Keys.Order() },
            Phases = { Phases.Keys.Order() },
            Scenes = { Scenes.Keys.Order() },
            Labels =
            {
                Labels
                    .Select(x => (x.Key, new LogicTools.LabelInfo { Target = x.Value.Target }))
                    .OrderBy(x => x.Key)
            },
            Characters = { Characters.Keys.Order() },
            Votings =
            {
                Votings.Select(x =>
                    (x.Key, new VotingInfo
                    {
                        UsedOptions =
                        {
                            x.Value.Funcs.Select(x => x.Code)
                            .SelectMany(x => SearchForUsedOptions(x))
                            .Distinct()
                            .Order()
                        }
                    })
                ).Order()
            },
            Options = { Options.Keys.Order() },
            Sequences =
            {
                Sequences.Select(x =>
                    (x.Key, new SequenceInfo { Steps = { x.Value.Steps.Select(y => y.Id.Text).Order() } })
                )
                .Order()
            },
            Events = { Events.Keys.Order() },
            PlayerNotification =
            {
                new List<CodeBlock>
                {
                    Phases.SelectMany(x => x.Value.Funcs.Select(y => y.Code)),
                    Scenes.SelectMany(x => x.Value.Funcs.Select(y => y.Code)),
                    Labels.SelectMany(x => x.Value.Funcs.Select(y => y.Code)),
                    Characters.SelectMany(x => x.Value.Funcs.Select(y => y.Code)),
                    Votings.SelectMany(x => x.Value.Funcs.Select(y => y.Code)),
                    Wins.SelectMany(x => x.Value.Funcs.Select(y => y.Code)),
                    Sequences.SelectMany(x => x.Value.Steps.Select(y => y.Code)),
                    Events.SelectMany(x => x.Value.Funcs.Select(y => y.Code)),
                    Events.SelectMany(x => x.Value.Targets.SelectMany(y => y.Steps.Select(z => z.Code))),
                }
                .Cast<IAstQueryable>()
                .SelectMany(x => x.GetAll(y => y is NotifyPlayerStatement))
                .Cast<NotifyPlayerStatement>()
                .Select(x => x.Name.Text)
                .Distinct()
                .Order()
            }
        };
    }

    public void Write(Configuration config, Output output)
    {
        WriteBanner(output);
        if (config.NameSpace is not null)
        {
            output.WriteLine($"namespace {config.NameSpace};");
            output.WriteLine();
        }
        foreach (var (_, mode) in Modes)
            if (!mode.IsAbstract)
                Write(output, mode);
        foreach (var (_, phase) in Phases)
            if (!phase.IsAbstract)
                Write(output, phase);
        foreach (var (_, scene) in Scenes)
            if (!scene.IsAbstract)
                Write(output, scene);
        foreach (var (_, label) in Labels)
            if (!label.IsAbstract)
                Write(output, label);
        foreach (var (_, character) in Characters)
            if (!character.IsAbstract)
                Write(output, character);
        foreach (var (_, voting) in Votings)
            if (!voting.IsAbstract)
                Write(output, voting);
        foreach (var (_, option) in Options)
            if (!option.IsAbstract)
                Write(output, option);
        foreach (var (_, win) in Wins)
            if (!win.IsAbstract)
                Write(output, win);
        foreach (var (_, sequence) in Sequences)
            if (!sequence.IsAbstract)
                Write(output, sequence);
        foreach (var (_, @event) in Events)
            if (!@event.IsAbstract)
                Write(output, @event);
    }

    private static void WriteBanner(Output output)
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        output.WriteLine($"//------------------------------------------------------------------------------");
        output.WriteLine($"// <auto-generated>");
        output.WriteLine($"//     This code was generated by a tool.");
        output.WriteLine($"//     LogicCompiler: {version}");
        output.WriteLine($"//");
        output.WriteLine($"//     Changes to this file may cause incorrect behavior and will be lost if");
        output.WriteLine($"//     the code is regenerated.");
        output.WriteLine($"// </auto-generated>");
        output.WriteLine($"//------------------------------------------------------------------------------");
        output.WriteLine();
        output.WriteLine("#nullable enable");
        output.WriteLine();
        output.WriteLine("using Werewolf.Theme;");
        output.WriteLine();
    }

    private void Write(Output output, Ast.ModeNode mode)
    {
        mode = mode.Resolve(Modes);
        output.WriteLine($"public sealed class Mode_{mode.Name.Text} : GameMode");
        output.WriteBlockBegin();
        output.WriteLine($"public Mode_{mode.Name.Text}(GameRoom? game, Werewolf.User.UserFactory users) : base(game, users)");
        output.WriteLine("{");
        output.WriteLine("}");
        output.WriteLine();

        output.WriteLine("public override Phase? GetStartPhase(GameRoom game)");
        output.WriteBlockBegin();
        output.WriteLine("var phases = new List<Phase>();");
        foreach (var (phase, _) in Phases)
        {
            output.WriteLine($"if (Phase_{phase}.IsStartPhase(game))");
            output.Push();
            output.WriteLine($"phases.Add(new Phase_{phase}(game));");
            output.Pop();
        }
        output.WriteLine("return phases.Count == 1 ? phases[0] : null;");
        output.WriteBlockEnd();

        output.Write("private static readonly List<string> character = new()");
        output.WriteBlockBegin();
        foreach (var character in mode.Character)
        {
            output.WriteLine($"\"{character.Text}\",");
        }
        output.Pop();
        output.WriteLine("};");
        output.WriteLine();
        output.WriteLine("public override IEnumerable<string> GetCharacterNames()");
        output.WriteBlockBegin();
        output.WriteLine("return character;");
        output.WriteBlockEnd();
        output.WriteLine();

        output.Write("private static readonly Type[] events = ");
        output.WriteCommaSeparatedList("{", Events.Keys.Select(x => $"typeof(Event_{x})"), "}");
        output.WriteLine(";");
        output.WriteLine();
        output.WriteLine("public override ReadOnlySpan<Type> GetEvents()");
        output.WriteBlockBegin();
        output.WriteLine("return events;");
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("public override Character? CreateCharacter(string name)");
        output.Push();
        output.WriteLine("=> name switch");
        output.WriteBlockBegin();
        foreach (var character in mode.Character)
        {
            output.WriteLine($"\"{character.Text}\" => new Character_{character.Text}(this),");
        }
        output.WriteLine("_ => null,");
        output.Pop();
        output.WriteLine("};");
        output.Pop();
        output.WriteLine();

        output.WriteLine("public override string? GetCharacterName(Type type)");
        output.WriteBlockBegin();
        foreach (var character in mode.Character)
        {
            output.WriteLine($"if (type == typeof(Character_{character.Text}))");
            output.Push();
            output.WriteLine($"return \"{character.Text}\";");
            output.Pop();
        }
        output.WriteLine("return null;");
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("public override IEnumerable<WinConditionCheck> GetWinConditions()");
        output.WriteBlockBegin();
        if (mode.Win.Count == 0)
            output.WriteLine("return [];");
        foreach (var win in mode.Win)
        {
            output.WriteLine($"yield return Win_{win.Text}.Check;");
        }
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("public override bool IsEnabled()");
        output.WriteBlockBegin();
        output.Write("return Game is not null");
        output.Push();
        foreach (var character in mode.Character)
        {
            output.WriteLine();
            output.Write($"&& (!Game.AllCharacters.Any(x => x is Character_{character.Text}) || Character_{character.Text}.IsEnabled(Game))");
        }
        output.Pop();
        output.WriteLine(";");
        output.WriteBlockEnd();

        output.WriteLine("public override bool CheckRoleUsage(string character, ref int count, int oldCount, [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out string? error)");
        output.WriteBlockBegin();
        output.WriteLine("if (oldCount == count)");
        output.WriteBlockBegin();
        output.WriteLine("error = null;");
        output.WriteLine("return true;");
        output.WriteBlockEnd();
        output.WriteLine("if (!base.CheckRoleUsage(character, ref count, oldCount, out error))");
        output.Push();
        output.WriteLine("return false;");
        output.Pop();
        output.WriteLine("switch (character)");
        output.WriteBlockBegin();
        foreach (var character in mode.Character)
        {
            output.WriteLine($"case \"{character.Text}\":");
            output.Push();
            output.WriteLine($"return Character_{character.Text}.ValidUsage(ref count, oldCount, out error);");
            output.Pop();
        }
        output.WriteLine("default:");
        output.Push();
        output.WriteLine("error = null;");
        output.WriteLine("return true;");
        output.Pop();
        output.WriteBlockEnd();
        output.WriteBlockEnd();

        output.WriteBlockEnd();
        output.WriteLine();
    }

    private void Write(Output output, Ast.PhaseNode phase)
    {
        phase = phase.Resolve(Phases);
        output.WriteLine($"public sealed class Phase_{phase.Name.Text} : Phase");
        output.WriteBlockBegin();
        output.WriteLine("private readonly GameRoom game;");
        output.WriteLine();
        output.WriteLine($"public Phase_{phase.Name.Text}(GameRoom game)");
        output.WriteBlockBegin();
        output.WriteLine("this.game = game;");
        var scenes = Flow?.GetSceneOrder(phase.Name.Text) ?? [];
        foreach (var scene in scenes)
        {
            output.WriteLine($"if (Scene_{scene}.Enabled(game))");
            output.Push();
            output.WriteLine($"EnabledScenes.Add(new Scene_{scene}());");
            output.Pop();
        }
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine($"public override string LanguageId => \"{phase.Name.Text}\";");
        output.WriteLine();
        output.WriteLine($"public override string BackgroundId");
        output.WriteBlockBegin();
        output.WriteLine("get");
        output.WriteBlockBegin();
        phase.Get("background")?.Code.Write(output, true);
        output.WriteBlockEnd();
        output.WriteBlockEnd();
        output.WriteLine();
        output.WriteLine($"public override string ColorTheme");
        output.WriteBlockBegin();
        output.WriteLine("get");
        output.WriteBlockBegin();
        phase.Get("theme")?.Code.Write(output, true);
        output.WriteBlockEnd();
        output.WriteBlockEnd();

        output.WriteLine("public static bool IsStartPhase(GameRoom game)");
        output.WriteBlockBegin();
        var func = phase.Get("start");
        if (func is not null)
            func.Code.Write(output, true);
        else output.WriteLine("return false;");
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("public override Phase? Next(GameRoom game)");
        output.WriteBlockBegin();
        output.WriteLine("return System.Activator.CreateInstance(GetNextPhaseType(game), game) as Phase;");
        output.WriteBlockEnd();
        output.WriteLine();
        output.WriteLine("private Type GetNextPhaseType(GameRoom game)");
        output.WriteBlockBegin();
        phase.Get("next")?.Code.Write(output, true);
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteBlockEnd();
        output.WriteLine();
    }

    private void Write(Output output, Ast.SceneNode scene)
    {
        scene = scene.Resolve(Scenes);
        output.WriteLine($"public sealed class Scene_{scene.Name.Text} : Scene");
        output.WriteBlockBegin();

        output.WriteLine("public static bool Enabled(GameRoom game)");
        output.WriteBlockBegin();
        var func = scene.Get("enable");
        if (func is not null)
            Write(output, func.Code);
        else output.WriteLine("return true;");
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("public override bool CanExecute(GameRoom game)");
        output.WriteBlockBegin();
        func = scene.Get("run_on");
        if (func is not null)
            Write(output, func.Code);
        else output.WriteLine("return true;");
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("public override bool CanMessage(GameRoom game, Character character)");
        output.WriteBlockBegin();
        func = scene.Get("can_message");
        if (func is not null)
            Write(output, func.Code);
        else output.WriteLine("return true;");
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("public override void Init(GameRoom game)");
        output.WriteBlockBegin();
        output.WriteLine("base.Init(game);");
        func = scene.Get("start");
        if (func is not null)
            Write(output, func.Code);
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("public override void Exit(GameRoom game)");
        output.WriteBlockBegin();
        output.WriteLine("base.Exit(game);");
        func = scene.Get("stop");
        if (func is not null)
            Write(output, func.Code);
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine($"public override string LanguageId => \"{scene.Name.Text}\";");

        output.WriteBlockEnd();
        output.WriteLine();
    }
    private void Write(Output output, Ast.LabelNode label)
    {
        label = label.Resolve(Labels);
        output.Write($"public sealed class Label_{label.Name.Text} : Werewolf.Theme.Labels.ILabel");
        if (label.Target.HasFlag(LabelTarget.Character))
            output.Write(", Werewolf.Theme.Labels.ICharacterLabel");
        if (label.Target.HasFlag(LabelTarget.Scene))
            output.Write(", Werewolf.Theme.Labels.ISceneLabel");
        if (label.Target.HasFlag(LabelTarget.Voting))
            output.Write(", Werewolf.Theme.Labels.IVotingLabel");
        if (label.Target.HasFlag(LabelTarget.Phase))
            output.Write(", Werewolf.Theme.Labels.IPhaseLabel");
        if (label.Target.HasFlag(LabelTarget.Mode))
            output.Write(", Werewolf.Theme.Labels.IGameRoomLabel");
        output.WriteBlockBegin();
        if (label.Withs.Count > 0)
        {
            foreach (var with in label.Withs)
            {
                output.Write("public ");
                with.GetValueType().Write(output);
                output.WriteLine($" Member_{with.Name.Text} {{ get; }}");
            }
            output.WriteLine();
            output.Write($"public Label_{label.Name.Text}(");
            bool first = true;
            foreach (var with in label.Withs)
            {
                if (first)
                    first = false;
                else output.Write(", ");
                with.GetValueType().Write(output);
                output.Write($" {with.Name.Text}");
            }
            output.WriteLine(")");
            output.WriteBlockBegin();
            foreach (var with in label.Withs)
            {
                output.WriteLine($"Member_{with.Name.Text} = {with.Name.Text};");
            }
            output.WriteBlockEnd();
            output.WriteLine();
        }

        output.WriteLine("public override bool Equals(object? obj)");
        output.WriteBlockBegin();
        output.Write($"return obj is Label_{label.Name.Text} other");
        foreach (var with in label.Withs)
        {
            output.WriteLine();
            output.Push();
            output.Write($"&& Member_{with.Name.Text}.Equals(other.Member_{with.Name.Text})");
            output.Pop();
        }
        output.WriteLine(";");
        output.WriteBlockEnd();
        output.WriteLine();
        output.WriteLine("public override int GetHashCode()");
        output.WriteBlockBegin();
        output.WriteLine("var hash = new HashCode();");
        output.WriteLine($"hash.Add(nameof(Label_{label.Name.Text}));");
        foreach (var with in label.Withs)
        {
            output.WriteLine($"hash.Add(Member_{with.Name.Text});");
        }
        output.WriteLine("return hash.ToHashCode();");
        output.WriteBlockEnd();
        output.WriteLine();

        if (label.Target.HasFlag(LabelTarget.Character))
        {
            output.WriteLine("public List<Character> Visible { get; } = [];");
            output.WriteLine();
            output.WriteLine($"public string Name => \"{label.Name.Text}\";");
            output.WriteLine();
            output.WriteLine("public bool CanLabelBeSeen(GameRoom game, Character current, Character? viewer)");
            output.Push();
            output.WriteLine($"=> viewer is null ?");
            output.Push();
            output.WriteLine($"CanLabelBeSeen(game, current, new OneOf.Types.None()) :");
            output.WriteLine($"CanLabelBeSeen(game, current, (OneOf.OneOf<Character, OneOf.Types.None>)viewer!);");
            output.Pop();
            output.Pop();
            output.WriteLine();
            output.WriteLine("private bool CanLabelBeSeen(GameRoom game, Character current, OneOf.OneOf<Character, OneOf.Types.None> viewer)");
            output.WriteBlockBegin();
            output.WriteLine("if (@viewer.TryPickT0(out var @_character, out _) && this.Visible.Contains(@_character))");
            output.Push();
            output.WriteLine("return true;");
            output.Pop();
            var func = label.Get("view");
            if (func is not null)
                Write(output, func.Code);
            else output.WriteLine("return false;");
            output.WriteBlockEnd();
            output.WriteLine();
        }

        var groups = new List<(LabelTarget, string, string)>
        {
            (LabelTarget.Phase, "phase", "Phase"),
            (LabelTarget.Scene, "scene", "Scene"),
            (LabelTarget.Character, "character", "Character"),
            (LabelTarget.Voting, "voting", "Voting"),
            (LabelTarget.Mode, "mode", "GameRoom"),
        };

        foreach (var (target, name, funcName) in groups)
        {
            if (!label.Target.HasFlag(target))
                continue;

            if (target == LabelTarget.Mode)
                output.WriteLine($"public void OnAttach{funcName}(GameRoom game, Werewolf.Theme.Labels.I{funcName}Label label)");
            else
                output.WriteLine($"public void OnAttach{funcName}(GameRoom game, Werewolf.Theme.Labels.I{funcName}Label label, {funcName} target)");
            output.WriteBlockBegin();
            var func = label.Get($"attach_{name}");
            if (func is not null)
                Write(output, func.Code);
            output.WriteBlockEnd();
            output.WriteLine();

            if (target == LabelTarget.Mode)
                output.WriteLine($"public void OnDetach{funcName}(GameRoom game, Werewolf.Theme.Labels.I{funcName}Label label)");
            else
                output.WriteLine($"public void OnDetach{funcName}(GameRoom game, Werewolf.Theme.Labels.I{funcName}Label label, {funcName} target)");
            output.WriteBlockBegin();
            func = label.Get($"detach_{name}");
            if (func is not null)
                Write(output, func.Code);
            output.WriteBlockEnd();
            output.WriteLine();
        }

        output.WriteBlockEnd();
        output.WriteLine();
    }

    private void Write(Output output, Ast.CharacterNode character)
    {
        character = character.Resolve(Characters);
        output.WriteLine($"public sealed class Character_{character.Name.Text} : Character");
        output.WriteBlockBegin();
        output.WriteLine($"public override string Name => \"{character.Name.Text}\";");
        output.WriteLine();
        output.WriteLine($"public Character_{character.Name.Text}(GameMode mode) : base(mode)");
        output.WriteBlockBegin();
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("public override Type ViewRole(GameRoom game, Character viewer)");
        output.WriteBlockBegin();
        var func = character.Get("view");
        if (func is not null)
            Write(output, func.Code);
        else output.WriteLine("return GetType();");
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("public static bool IsEnabled(GameRoom game)");
        output.WriteBlockBegin();
        func = character.Get("enable");
        if (func is not null)
            Write(output, func.Code);
        else output.WriteLine("return true;");
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("public static bool ValidUsage(ref int count, int oldCount, [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out string? error)");
        output.WriteBlockBegin();
        func = character.Get("valid_usage");
        if (func is not null)
        {
            output.WriteLine("var corrected = ValidUsage(count, oldCount);");
            output.WriteLine("if (corrected is < 0 or > int.MaxValue)");
            output.WriteBlockBegin();
            output.WriteLine($"error = $\"invalid value {{corrected}} for corrected number [{character.Name.Text}]\";");
            output.WriteLine("count = oldCount;");
            output.WriteLine("return false;");
            output.WriteBlockEnd();
            output.WriteLine("if (corrected != count)");
            output.WriteBlockBegin();
            output.WriteLine($"error = $\"{{count}} is not a valid number of {character.Name.Text}\";");
            output.WriteLine("count = (int)corrected;");
            output.WriteLine("return false;");
            output.WriteBlockEnd();
            output.WriteLine("error = null;");
            output.WriteLine("return true;");
            output.WriteBlockEnd();
            output.WriteLine();

            output.WriteLine("private static long ValidUsage(long count, long old)");
            output.WriteBlockBegin();
            Write(output, func.Code);
        }
        else
        {
            output.WriteLine("error = null;");
            output.WriteLine("return true;");
        }
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("public override void Init(GameRoom game)");
        output.WriteBlockBegin();
        output.WriteLine("base.Init(game);");
        func = character.Get("create");
        if (func is not null)
            Write(output, func.Code);
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteBlockEnd();
        output.WriteLine();
    }

    private void Write(Output output, Ast.VotingNode voting)
    {
        voting = voting.Resolve(Votings);
        output.WriteLine($"public sealed class Voting_{voting.Name.Text} : Voting");
        output.WriteBlockBegin();
        output.WriteLine("private readonly Dictionary<int, VoteOption> options = new();");
        output.WriteLine("private readonly HashSet<Werewolf.User.UserId>? eligable;");
        output.WriteLine("private readonly HashSet<Werewolf.User.UserId>? viewer;");
        if (voting.Target is VotingTarget.Each or VotingTarget.MultiEach)
            output.WriteLine("private readonly Character target;");
        if (voting.Target is VotingTarget.MultiEach)
            output.WriteLine("private int remainingOpt = 1;");
        output.WriteLine();
        output.WriteLine($"public Voting_{voting.Name.Text}(");
        output.Push();
        output.WriteLine($"GameRoom game,");
        if (voting.Target is VotingTarget.Each or VotingTarget.MultiEach)
            output.WriteLine("Character target,");
        output.WriteLine($"IEnumerable<VoteOption>? choices = null,");
        output.WriteLine($"IEnumerable<Character>? eligable = null,");
        output.WriteLine($"IEnumerable<Character>? viewer = null");
        output.Pop();
        output.WriteLine($") : base(game)");
        output.WriteBlockBegin();
        if (voting.Target is VotingTarget.Each or VotingTarget.MultiEach)
            output.WriteLine("this.target = target;");
        output.WriteLine("choices ??= GetDefaultOptions(game);");
        output.WriteLine("int id = 0;");
        output.WriteLine("foreach (var choice in choices)");
        output.WriteBlockBegin();
        output.WriteLine("choice.Users.Clear();");
        output.WriteLine("this.options.Add(id++, choice);");
        output.WriteBlockEnd();
        output.WriteLine($"if (eligable != null)");
        output.WriteBlockBegin();
        output.WriteLine($"this.eligable = [];");
        output.WriteLine($"foreach (var character in eligable)");
        output.WriteBlockBegin();
        output.WriteLine($"if (game.TryGetId(character) is Werewolf.User.UserId uid)");
        output.Push();
        output.WriteLine($"_ = this.eligable.Add(uid);");
        output.Pop();
        output.WriteBlockEnd();
        output.WriteBlockEnd();
        output.WriteLine($"if (viewer != null)");
        output.WriteBlockBegin();
        output.WriteLine($"this.viewer = [];");
        output.WriteLine($"foreach (var character in viewer)");
        output.WriteBlockBegin();
        output.WriteLine($"if (game.TryGetId(character) is Werewolf.User.UserId uid)");
        output.Push();
        output.WriteLine($"_ = this.viewer.Add(uid);");
        output.Pop();
        output.WriteBlockEnd();
        output.WriteBlockEnd();
        output.WriteLine();
        output.WriteLine("Create(game);");
        output.WriteBlockEnd();
        output.WriteLine();

        if (voting.Target is VotingTarget.MultiEach)
        {
            output.WriteLine("protected override int GetMissingVotes(GameRoom game)");
            output.WriteBlockBegin();
            output.WriteLine("return remainingOpt;");
            output.WriteBlockEnd();
            output.WriteLine();

            output.WriteLine("public override string? Vote(GameRoom game, Werewolf.User.UserId voter, int id)");
            output.WriteBlockBegin();
            output.WriteLine("if (id == -1)");
            output.WriteBlockBegin();
            output.WriteLine("var opts = options.Values.Where(x => x.Users.Count > 0).ToList();");
            output.WriteLine("var chars = opts.Where(x => x is CharacterOption).Cast<CharacterOption>().Select(x => x.Character).ToList();");
            output.WriteLine("if (!CanFinish(game, opts, chars))");
            output.Push();
            output.WriteLine("return \"cannot stop voting right now\";");
            output.Pop();
            output.WriteLine("remainingOpt = 0;");
            output.WriteLine("game.SendEvent(new Werewolf.Theme.Events.SetVotingVote(this, id, voter));");
            output.WriteLine("CheckVotingFinished(game);");
            output.WriteLine("return null;");
            output.WriteBlockEnd();
            output.WriteLine();
            output.WriteLine("if (!options.TryGetValue(id, out var option))");
            output.Push();
            output.WriteLine("return \"option not found\";");
            output.Pop();
            output.WriteLine();
            output.WriteLine("if (option.Users.Contains(voter))");
            output.WriteBlockBegin();
            output.WriteLine("var existing = option.Users.Where(x => x != voter).ToList();");
            output.WriteLine("option.Users.Clear();");
            output.WriteLine("foreach (var user in existing)");
            output.Push();
            output.WriteLine("option.Users.Add(user);");
            output.Pop();
            output.WriteLine("game.SendEvent(new Werewolf.Theme.Events.RemoveVotingVote(this, id, voter));");
            output.WriteBlockEnd();
            output.WriteLine("else");
            output.WriteBlockBegin();
            output.WriteLine("option.Users.Add(voter);");
            output.WriteLine("game.SendEvent(new Werewolf.Theme.Events.SetVotingVote(this, id, voter));");
            output.WriteBlockEnd();
            output.WriteLine();
            output.WriteLine("CheckVotingFinished(game);");
            output.WriteLine("return null;");
            output.WriteBlockEnd();
            output.WriteLine();
        }

        output.Write("public override IEnumerable<(int id, VoteOption option)> Options => options.Select(x => (x.Key, x.Value))");
        if (voting.Target is VotingTarget.MultiEach)
            output.Write(".Append((-1, new VoteOption(\"stop-voting\")))");
        output.WriteLine(";");
        output.WriteLine();
        output.WriteLine($"public override string LanguageId => \"{voting.Name.Text}\";");
        output.WriteLine();

        output.WriteLine("public static IEnumerable<Character> GetTargets(GameRoom game)");
        output.WriteBlockBegin();
        var func = voting.Get("targets");
        if (func is not null)
            Write(output, func.Code);
        else output.WriteLine("return Enumerable.Empty<Character>();");
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("private IEnumerable<VoteOption> GetDefaultOptions(GameRoom game)");
        output.WriteBlockBegin();
        func = voting.Get("voting_option");
        if (func is not null)
            Write(output, func.Code);
        else output.WriteLine("return Enumerable.Empty<VoteOption>();");
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("public override bool CanView(GameRoom game, Character viewer)");
        output.WriteBlockBegin();
        if (voting.Target is VotingTarget.Each or VotingTarget.MultiEach)
        {
            output.WriteLine("if (viewer == this.target)");
            output.Push();
            output.WriteLine("return true;");
            output.Pop();
        }
        output.WriteLine("if (this.viewer != null && game.TryGetId(viewer) is Werewolf.User.UserId _uid)");
        output.Push();
        output.WriteLine("return this.viewer.Contains(_uid);");
        output.Pop();
        func = voting.Get("can_view");
        if (func is not null)
            Write(output, func.Code);
        else output.WriteLine("return true;");
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("protected override bool CanVoteBase(GameRoom game, Character voter)");
        output.WriteBlockBegin();
        if (voting.Target is VotingTarget.Each or VotingTarget.MultiEach)
        {
            output.WriteLine("if (voter == this.target)");
            output.Push();
            output.WriteLine("return true;");
            output.Pop();
        }
        output.WriteLine("if (this.eligable != null && game.TryGetId(voter) is Werewolf.User.UserId _uid)");
        output.Push();
        output.WriteLine("return this.eligable.Contains(_uid);");
        output.Pop();
        func = voting.Get("can_vote");
        if (func is not null)
            Write(output, func.Code);
        else output.WriteLine("return true;");
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine("private void Create(GameRoom game)");
        output.WriteBlockBegin();
        func = voting.Get("create");
        if (func is not null)
            Write(output, func.Code);
        output.WriteBlockEnd();
        output.WriteLine();

        if (voting.Target is VotingTarget.MultiEach)
        {
            output.WriteLine("private bool CanFinish(GameRoom game, List<VoteOption> choices, List<Character> characters)");
            output.WriteBlockBegin();
            func = voting.Get("can_finish");
            if (func is not null)
                Write(output, func.Code);
            else output.WriteLine("return true;");
            output.WriteBlockEnd();
            output.WriteLine();

            output.WriteLine("protected override void Execute(GameRoom game, int id)");
            output.WriteBlockBegin();
            output.WriteLine("var option = options[id];");
            output.WriteLine("if (option is CharacterOption chOption)");
            output.Push();
            output.WriteLine("ExecuteUnanimous(game, [option], [chOption.Character]);");
            output.Pop();
            output.WriteLine("else");
            output.Push();
            output.WriteLine("ExecuteUnanimous(game, [option], []);");
            output.Pop();
            output.WriteBlockEnd();
            output.WriteLine();
        }
        else
        {
            output.WriteLine("protected override void Execute(GameRoom game, int id)");
            output.WriteBlockBegin();
            output.WriteLine("var option = options[id];");
            output.WriteLine("if (option is CharacterOption chOption)");
            output.Push();
            output.WriteLine("ExecuteChoice(game, option, chOption.Character);");
            output.Pop();
            output.WriteLine("else");
            output.Push();
            output.WriteLine("ExecuteChoice(game, option, new OneOf.Types.None());");
            output.Pop();
            output.WriteBlockEnd();
            output.WriteLine();
            output.WriteLine("private void ExecuteChoice(GameRoom game, VoteOption choice, OneOf.OneOf<Character, OneOf.Types.None> character)");
            output.WriteBlockBegin();
            func = voting.Get("choice");
            if (func is not null)
                Write(output, func.Code);
            output.WriteBlockEnd();
            output.WriteLine();
        }

        output.WriteLine("protected override void Execute(GameRoom game, IEnumerable<int> ids)");
        output.WriteBlockBegin();
        output.WriteLine("var options = ids");
        output.Push();
        output.WriteLine(".Where(x => x >= 0)");
        output.WriteLine(".Select(x => this.options[x])");
        output.WriteLine(".Where(x => x.Users.Count > 0)");
        output.WriteLine(".ToList();");
        output.Pop();
        output.WriteLine("var characters = options");
        output.Push();
        output.WriteLine(".Where(x => x is CharacterOption)");
        output.WriteLine(".Cast<CharacterOption>()");
        output.WriteLine(".Select(x => x.Character)");
        output.WriteLine(".ToList();");
        output.Pop();
        output.WriteLine("ExecuteUnanimous(game, options, characters);");
        output.WriteBlockEnd();
        output.WriteLine();
        output.WriteLine("private void ExecuteUnanimous(GameRoom game, List<VoteOption> choices, List<Character> characters)");
        output.WriteBlockBegin();
        func = voting.Get("unanimous");
        if (func is not null)
            Write(output, func.Code);
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteBlockEnd();
        output.WriteLine();
    }

    private static void Write(Output output, Ast.OptionNode option)
    {
        output.WriteLine($"public sealed class Option_{option.Name.Text} : VoteOption");
        output.WriteBlockBegin();
        output.WriteLine($"public Option_{option.Name.Text}() : base(\"{option.Name.Text}\")");
        output.WriteBlockBegin();
        output.WriteBlockEnd();
        output.WriteBlockEnd();
        output.WriteLine();
    }

    private static void Write(Output output, Ast.WinNode win)
    {
        output.WriteLine($"public static class Win_{win.Name.Text}");
        output.WriteBlockBegin();
        output.WriteLine($"public static bool Check(GameRoom game, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ReadOnlyMemory<Character>? winner)");
        output.WriteBlockBegin();

        var func = win.Get("has_one");
        if (func is not null)
        {
            output.WriteLine("return (winner = HasOne(game).ToArray()).Value.Length > 0;");
            output.WriteBlockEnd();
            output.WriteLine();

            output.WriteLine($"private static IEnumerable<Character> HasOne(GameRoom game)");
            output.WriteBlockBegin();
            Write(output, func.Code);
            output.WriteBlockEnd();

            output.WriteBlockEnd();
            output.WriteLine();
            return;
        }

        output.WriteLine("if (!CheckCondition(game))");
        output.WriteBlockBegin();
        output.WriteLine("winner = null;");
        output.WriteLine("return false;");
        output.WriteBlockEnd();
        output.WriteLine("winner = GetWinner(game).ToArray();");
        output.WriteLine("return true;");
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine($"private static bool CheckCondition(GameRoom game)");
        output.WriteBlockBegin();
        func = win.Get("check");
        if (func is not null)
            Write(output, func.Code);
        else output.WriteLine("return false;");
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine($"private static IEnumerable<Character> GetWinner(GameRoom game)");
        output.WriteBlockBegin();
        func = win.Get("winner");
        if (func is not null)
            Write(output, func.Code);
        else output.WriteLine("return Enumerable.Empty<Character>();");
        output.WriteBlockEnd();

        output.WriteBlockEnd();
        output.WriteLine();
    }

    private static void Write(Output output, Ast.SequenceNode sequence)
    {
        output.WriteLine($"public sealed class Sequence_{sequence.Name.Text} : Sequence");
        output.WriteBlockBegin();
        output.WriteLine("private readonly Character target;");
        output.WriteLine();
        output.WriteLine($"public Sequence_{sequence.Name.Text}(GameRoom game, Character target)");
        output.WriteBlockBegin();
        output.WriteLine("this.target = target;");
        output.WriteBlockEnd();
        output.WriteLine();
        output.WriteLine($"public override string Name => \"{sequence.Name.Text}\";");
        output.WriteLine();
        output.WriteLine("public override void WriteMeta(System.Text.Json.Utf8JsonWriter writer, GameRoom game, Werewolf.User.UserInfo target)");
        output.WriteBlockBegin();
        output.WriteLine("var id = game.TryGetId(this.target);");
        output.WriteLine("if (id is not null && game.Users.TryGetValue(id.Value, out var profile))");
        output.Push();
        output.WriteLine("writer.WriteString(\"target\", profile.User.Config.Username);");
        output.Pop();
        output.WriteBlockEnd();
        output.WriteLine();

        Write(output, sequence.Steps);

        output.WriteBlockEnd();
        output.WriteLine();
    }

    private static void Write(Output output, List<Step> steps)
    {
        output.WriteLine($"public override int MaxStep => {steps.Count};");
        output.WriteLine();
        output.WriteLine($"public override void Continue(GameRoom game)");
        output.WriteBlockBegin();
        if (steps.Count > 0)
        {
            output.WriteLine("switch (Step)");
            output.WriteBlockBegin();
            for (int i = 0; i < steps.Count; i++)
            {
                output.WriteLine($"case {i}: Do{steps[i].Id.Text}(game); break;");
            }
            output.WriteLine("default: Stop(); return;");
            output.WriteBlockEnd();
            output.WriteLine("Next();");
        }
        else
        {
            output.WriteLine("Stop();");
        }
        output.WriteBlockEnd();
        output.WriteLine();

        output.WriteLine($"protected override string? GetName(int step)");
        output.Push();
        output.WriteLine("=> step switch");
        output.WriteBlockBegin();
        for (int i = 0; i < steps.Count; i++)
        {
            output.WriteLine($"{i} => \"{steps[i].Id.Text}\",");
        }
        output.WriteLine("_ => null,");
        output.Pop();
        output.WriteLine("};");
        output.Pop();

        for (int i = 0; i < steps.Count; i++)
        {
            output.WriteLine();
            output.WriteLine($"private void Do{steps[i].Id.Text}(GameRoom game)");
            output.WriteBlockBegin();
            Write(output, steps[i].Code);
            output.WriteBlockEnd();
        }
    }

    private static void Write(Output output, Ast.EventNode @event)
    {
        output.WriteLine($"public sealed class Event_{@event.Name.Text} : Event");
        output.WriteBlockBegin();
        output.WriteLine("public override bool Enable(GameRoom game)");
        output.WriteBlockBegin();
        var func = @event.Get("enable");
        if (func is not null)
            Write(output, func.Code);
        else output.WriteLine("return true;");
        output.WriteBlockEnd();

        output.WriteLine("public override bool Finished(GameRoom game)");
        output.WriteBlockBegin();
        func = @event.Get("finished");
        if (func is not null)
            Write(output, func.Code);
        else output.WriteLine("return true;");
        output.WriteBlockEnd();

        {
            var target = @event.Targets.Find(x => x.TargetSpecifier is null && x.Target.Text == "now");
            if (target is not null)
            {
                output.WriteLine();
                output.WriteLine("public override Sequence? TargetNow");
                output.Push();
                output.WriteLine($"=> new Target_Normal_now();");
                output.Pop();
            }
        }

        output.WriteLine();
        output.WriteLine("public override Sequence? TargetPhase(Phase phase)");
        output.Push();
        output.WriteLine("=> phase switch");
        output.WriteBlockBegin();
        foreach (var target in @event.Targets.Where(x => x.TargetSpecifier == TypeSpecifier.Phase))
        {
            output.WriteLine($"Phase_{target.Target.Text} => new Target_Phase_{target.Target.Text}(),");
        }
        output.WriteLine("_ => null,");
        output.Pop();
        output.WriteLine("};");
        output.Pop();

        output.WriteLine();
        output.WriteLine("public override Sequence? TargetScene(Scene scene)");
        output.Push();
        output.WriteLine("=> scene switch");
        output.WriteBlockBegin();
        foreach (var target in @event.Targets.Where(x => x.TargetSpecifier == TypeSpecifier.Scene))
        {
            output.WriteLine($"Scene_{target.Target.Text} => new Target_Scene_{target.Target.Text}(),");
        }
        output.WriteLine("_ => null,");
        output.Pop();
        output.WriteLine("};");
        output.Pop();

        foreach (var target in @event.Targets)
        {
            output.WriteLine();
            output.WriteLine($"private sealed class Target_{target.TargetSpecifier?.ToString() ?? "Normal"}_{target.Target.Text} : Sequence");
            output.WriteBlockBegin();
            output.WriteLine($"public override string Name => \"{@event.Name.Text}+{target.TargetSpecifier?.ToString() ?? "Normal"}+{target.Target.Text}\";");
            output.WriteLine();

            Write(output, target.Steps);

            output.WriteBlockEnd();
        }

        output.WriteBlockEnd();
        output.WriteLine();
    }

    private static void Write(Output output, Ast.CodeBlock code)
    {
        code.Write(output);
    }
}
