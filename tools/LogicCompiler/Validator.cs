using LogicCompiler.Ast;

namespace LogicCompiler;

internal sealed class Validator(Generator data, Output? docs)
{
    private readonly Generator data = data;

    private readonly Output? docs = docs;

    public bool Validate()
    {
        CheckReferences(data.Modes);
        CheckReferences(data.Phases);
        CheckReferences(data.Scenes);
        CheckReferences(data.Labels);
        CheckReferences(data.Characters);
        CheckReferences(data.Votings);
        CheckReferences(data.Options);
        CheckReferences(data.Wins);
        CheckReferences(data.Sequences);
        CheckReferences(data.Events);

        RestrictNames(data.Phases, [
            new Method(this, "start", Ast.ValueType.Bool)
                .Doc(
                    """
                    Returns true if this is the current start phase. Exactly one phase has to return
                    true and all other ones has to return false. If this is not the case the mode is
                    not allowed to start in this configuration.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Phase),
            new Method(this, "next", Ast.ValueType.PhaseType)
                .Doc(
                    """
                    Returns the type of the next phase that will be executed after this one. There
                    is no need that the next phase has a scene that can be executed. The runner will
                    just ask for the next phase again. If during this process a phase is returned
                    twice and no executable scene could be found, the game is aborted and doesn't
                    count to any highscores. If you want to finish a game, you have to use the win
                    conditions. Win conditions are checked **before** any phase `next` call.
                    """)
                .Required()
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Phase),
            new Method(this, "background", Ast.ValueType.String)
                .Doc("The resource path to the background image for the current phase.")
                .Required()
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Phase),
            new Method(this, "theme", Ast.ValueType.String)
                .Doc("The current color theme for this phase. Must be in the rgb format '#abcdef'.")
                .Required()
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Phase),
        ]);
        RestrictNames(data.Scenes, [
            new Method(this, "enable", Ast.ValueType.Bool)
                .Doc(
                    """
                    Returns true if this scene is enabled for this game mode and configuration at
                    all. This is only evaluated once before the start of the game.
                    """)
                .Add("$game", Ast.ValueType.Mode),
            new Method(this, "run_on", Ast.ValueType.Bool)
                .Doc(
                    """
                    Checks if this scene can be executed right now. If not the game cycle will check
                    the next scene and so on.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Scene),
            new Method(this, "can_message", Ast.ValueType.Bool)
                .Doc(
                    """
                    Returns true if `$character` can send a message in the current scene.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Scene)
                .Add("$character", Ast.ValueType.Character),
            new Method(this, "start", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc("Executed everytime when this scene starts")
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Scene),
            new Method(this, "stop", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc(
                    """
                    Executed everytime when this scene is finished and transitioned to the next one.
                    This wont be executed if the game was finished in this scene.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Scene),
        ]);
        RestrictNames(data.Labels, [
            new Method(this, "view", Ast.ValueType.Bool)
                .Doc(
                    """
                    Returns true, if this label can be seen by `$viewer`. This label is attached to
                    the character `$current`. If the viewer is the game master (without a character)
                    or a player that is disabled and is allowed to see details, `$viewer` is empty.
                    Guests that just joined the lobby cannot see any details and this method is
                    never called for them.

                    This method is only available if target is set to `character`.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Label)
                .Add("$current", Ast.ValueType.Character)
                .Add("$viewer", Ast.ValueType.Character | Ast.ValueType.Optional),
            new Method(this, "attach_character", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc(
                    """
                    The action that is executed if this label is attached to the character
                    `$target`.

                    This method is only available if target is set to `character`.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Label)
                .Add("$target", Ast.ValueType.Character),
            new Method(this, "attach_phase", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc(
                    """
                    The action that is executed if this label is attached to the phase `$target`.

                    This method is only available if target is set to `phase`.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Label)
                .Add("$target", Ast.ValueType.Phase),
            new Method(this, "attach_scene", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc(
                    """
                    The action that is executed if this label is attached to the scene `$target`.

                    This method is only available if target is set to `scene`.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Label)
                .Add("$target", Ast.ValueType.Scene),
            new Method(this, "attach_voting", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc(
                    """
                    The action that is executed if this label is attached to the voting `$target`.

                    This method is only available if target is set to `voting`.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Label)
                .Add("$target", Ast.ValueType.Voting),
            new Method(this, "detach_character", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc(
                    """
                    The action that is executed if this label is detached from the character
                    `$target`.

                    This method is only available if target is set to `character`.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Label)
                .Add("$target", Ast.ValueType.Character),
            new Method(this, "detach_phase", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc(
                    """
                    The action that is executed if this label is detached from the phase `$target`.

                    This method is only available if target is set to `phase`.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Label)
                .Add("$target", Ast.ValueType.Phase),
            new Method(this, "detach_scene", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc(
                    """
                    The action that is executed if this label is detached from the scene `$target`.

                    This method is only available if target is set to `scene`.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Label)
                .Add("$target", Ast.ValueType.Scene),
            new Method(this, "detach_voting", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc(
                    """
                    The action that is executed if this label is detached from the voting `$target`.

                    This method is only available if target is set to `voting`.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Label)
                .Add("$target", Ast.ValueType.Voting),
        ]);
        RestrictNames(data.Characters, [
            new Method(this, "enable", Ast.ValueType.Bool)
                .Doc(
                    """
                    Returns true if the current character configuration is allowed and the game can
                    be start. This method is called for each character prototype that is a part of
                    the current game. Non selected characters are not checked.

                    If this method is not defined, the current configuration will always be
                    accepted.
                    """)
                .Add("$game", Ast.ValueType.Mode),
            new Method(this, "create", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc(
                    """
                    Will be executed when this character is part of the game and the game starts.
                    At this point is this character always enabled.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Character),
            new Method(this, "view", Ast.ValueType.CharacterType)
                .Doc(
                    """
                    Returns the character type that is visible to the respective viewer. By default
                    this will return the current character type but you can specify this function to
                    overwrite this behavior and let them see a different character type. Can be used
                    to hide the character role from the player.

                    This function can be overwritten by the game rules `AllCanSeeRoleOfDead` and
                    `DeadCanSeeAllRoles`. The game master (if he has no character) and the character
                    itself can always see the true character type. Guests that just joined the lobby
                    can never see the real character.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Character)
                .Add("$viewer", Ast.ValueType.Character),
            new Method(this, "attach", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc("Will be executed if `$label` is added to this character.")
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Character)
                .Add("$label", Ast.ValueType.Label),
            new Method(this, "detach", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc("Will be executed if `$label` is removed from this character.")
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Character)
                .Add("$label", Ast.ValueType.Label),
        ]);
        RestrictNames(data.Votings, [
            new Method(this, "targets", Ast.ValueType.Character | Ast.ValueType.Collection)
                .Doc(
                    """
                    The list of character that are used as a target for this voting. Only used if
                    `target` is set to `each` or `multi_each`.

                    ```
                    voting VotingName {
                        target each;
                        // The list of all targets. For each one is a voting created that is
                        // assigned to only this character.
                        func targets { @character }
                    }
                    ```
                    """)
                .Add("$game", Ast.ValueType.Mode),
            new Method(this, "voting_option", Ast.ValueType.Character | Ast.ValueType.OptionType | Ast.ValueType.Collection)
                .Doc(
                    """
                    Returns a list of options that will be displayed if not otherwise specified at
                    spawn.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Voting)
                .Add<VotingNode>("$target", Ast.ValueType.Character, x => x.Target is VotingTarget.Each or VotingTarget.MultiEach),
            new Method(this, "can_vote", Ast.ValueType.Bool)
                .Doc(
                    """
                    Returns true if the character `$voter` is allowed to vote in this voting
                    instance. If `target each` or `target multi_each` was set, `$target` will
                    contain the target character for this voting. Otherwise, is the variable
                    `$target` not available.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Voting)
                .Add<VotingNode>("$target", Ast.ValueType.Character, x => x.Target is VotingTarget.Each or VotingTarget.MultiEach)
                .Add("$voter", Ast.ValueType.Character),
            new Method(this, "can_view", Ast.ValueType.Bool)
                .Doc(
                    """
                    Returns true if the character `$viewer` is allowed to view this voting instance.
                    If `target each` or `target multi_each` was set, `$target` will contain the
                    target character for this voting. Otherwise, is the variable `$target` not
                    available.

                    The game master (if he has no character) can always view any voting. Guests that
                    just joined the lobby and have no character can never see any voting.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Voting)
                .Add<VotingNode>("$target", Ast.ValueType.Character, x => x.Target is VotingTarget.Each or VotingTarget.MultiEach)
                .Add("$viewer", Ast.ValueType.Character),
            new Method(this, "create", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc("Executed when this voting is created and attached to the game.")
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Voting)
                .Add<VotingNode>("$target", Ast.ValueType.Character, x => x.Target is VotingTarget.Each or VotingTarget.MultiEach),
            new Method(this, "can_finish", Ast.ValueType.Bool)
                .Doc(
                    """
                    Checks if the current voting can be finished. Only used if `target multi_each`
                    was set. This function will be called everytime the submit button was pressed.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Voting)
                .Add("$target", Ast.ValueType.Character)
                .Add("$characters", Ast.ValueType.Character | Ast.ValueType.Collection)
                .Add("$choices", Ast.ValueType.Option | Ast.ValueType.Collection),
            new Method(this, "choice", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc(
                    """
                    Will be executed if a single option got the majority of all voter. `$choice`
                    contains the selected option. `$character` contains the selected character if
                    the selected option is one.

                    If multiple options have the majority, the function `unanimous` will be
                    executed. If the voting was cancelled prematurely, neither `choice` nor
                    `unanimous` will be executed.

                    If `target each` or `target multi_each` was set, `$target` will contain the
                    target character for this voting. Otherwise, is the variable `$target` not
                    available.

                    If `target multi_each` was set, a finalized voting will always call the handler
                    `func unanimous` and not this one.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Voting)
                .Add<VotingNode>("$target", Ast.ValueType.Character, x => x.Target is VotingTarget.Each or VotingTarget.MultiEach)
                .Add("$character", Ast.ValueType.Character | Ast.ValueType.Optional)
                .Add("$choice", Ast.ValueType.Option | Ast.ValueType.Character),
            new Method(this, "unanimous", Ast.ValueType.Void | Ast.ValueType.Mutable)
                .Doc(
                    """
                    Will be executed if multiple options got the majority of all voter. `$choices`
                    contain all the selected options. `$characters` contain the selected characters
                    out of `$choices`.

                    If only one option has the majority, the function `unanimous` will be executed.
                    If the voting was cancelled prematurely, neither `choice` nor `unanimous` will
                    be executed.

                    If `target each` or `target multi_each` was set, `$target` will contain the
                    target character for this voting. Otherwise, is the variable `$target` not
                    available.

                    If `target multi_each` was set, a finalized voting will always call this handler
                    and not `func choice`. The submit option wont be included in the resulting list.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Voting)
                .Add<VotingNode>("$target", Ast.ValueType.Character, x => x.Target is VotingTarget.Each or VotingTarget.MultiEach)
                .Add("$characters", Ast.ValueType.Character | Ast.ValueType.Collection)
                .Add("$choices", Ast.ValueType.Option | Ast.ValueType.Collection),
        ]);
        RestrictNames(data.Wins, [
            new Method(this, "check", Ast.ValueType.Bool)
                .Doc(
                    """
                    Returns true if the win condition is successfull and the game is now finished.
                    """)
                .Add("$game", Ast.ValueType.Mode),
            new Method(this, "winner", Ast.ValueType.Character | Ast.ValueType.Collection)
                .Doc(
                    """
                    Returns a collection of all character that won this game right now. If this
                    function is not defined, no winner is selected.
                    """)
                .Add("$game", Ast.ValueType.Mode),
            new Method(this, "has_one", Ast.ValueType.Character | Ast.ValueType.Collection)
                .Doc(
                    """
                    Checks if there is one or more winner. The winner will be returned using this
                    function. If an empty collection is returned, the game is considered as
                    unfinished and will be continued.

                    If this function is defined, it replaces the functions `check` and `winner`.
                    Those two will be ignored and only this will be handled.
                    """)
                .Add("$game", Ast.ValueType.Mode),
        ]);
        RestrictNames(data.Events, [
            new Method(this, "enable", Ast.ValueType.Bool)
                .Doc("Returns true if this event is allowed to be triggered at this time.")
                .Add("$game", Ast.ValueType.Mode),
            new Method(this, "finished", Ast.ValueType.Bool)
                .Doc(
                    """
                    Returns true if this event is finished and can be removed from the current event
                    list. As long as an event is still contained in the event list, all listed
                    sequences will trigger as long as the specified phase or scene is entered.
                    """)
                .Add("$game", Ast.ValueType.Mode)
                .Add("$this", Ast.ValueType.Event),
        ]);
        RestrictNames(data.Events, ["now"]);

        HandleModes(data.Modes);
        CheckSteps(data.Sequences);
        CheckSteps(data.Events);

        data.Flow = new Flow(data);

        return !Error.HasError;
    }

    private static void CheckReferences<T>(Dictionary<string, T> dict)
        where T : Ast.NodeBase
    {
        HashSet<string> finished = [];
        foreach (var (name, data) in dict)
        {
            if (data.Inherits.Count == 0 || finished.Contains(name))
            {
                _ = finished.Add(name);
                continue;
            }
            HashSet<string> active = [];
            Stack<(string, T)> jobs = [];
            jobs.Push((name, data));
            while (jobs.TryPop(out var jobInfo))
            {
                var (jobName, jobData) = jobInfo;
                // check if finished
                if (active.Remove(jobName))
                {
                    _ = finished.Add(jobName);
                    continue;
                }

                _ = active.Add(jobName);
                jobs.Push((jobName, jobData));
                // add dependencies
                foreach (var @base in jobData.Inherits)
                {
                    if (!dict.TryGetValue(@base.Text, out var depenData))
                    {
                        Error.WriteError(data.SourceFile, @base.Source, $"Base type `{@base.Text}` not found");
                        continue;
                    }
                    if (active.Contains(@base.Text))
                    {
                        Error.WriteError(data.SourceFile, @base.Source, $"Dependency loop found: {string.Join(" -> ", active)} -> {@base.Text}");
                        continue;
                    }
                    if (depenData.Inherits.Count == 0)
                    {
                        _ = finished.Add(@base.Text);
                        continue;
                    }
                    jobs.Push((@base.Text, depenData));
                }

            }
        }
    }

    private void RestrictNames<T>(Dictionary<string, T> dict, List<Method> methods)
        where T : ICodeContainer, ISourceNode
    {
        if (docs != null)
        {
            docs.WriteLine($"## {typeof(T).Name}");
            docs.WriteLine();
            foreach (var method in methods)
                method.WriteDoc(docs);
        }
        foreach (var (_, item) in dict)
        {
            foreach (var func in item.Funcs)
            {
                var method = methods.Find(x => x.Name == func.Id.Text);
                if (method is null)
                {
                    Error.WriteError(func.Id,
                        $"Unsupported name `{func.Id.Text}`. Expected: {string.Join(", ", methods.Select(x => $"`{x.Name}`"))}");
                    continue;
                }
                HandleCodeBlock(func, func.Code, method.GetContext(item), method.Type);
            }
            foreach (var method in methods.Where(x => x.IsRequired))
            {
                if (item.Funcs.Any(x => x.Id.Text == method.Name))
                    continue;
                Error.WriteError(item, $"required function `{method.Name}` not defined");
            }
        }
    }

    private void RestrictNames(Dictionary<string, EventNode> dict, HashSet<string> allowedNames)
    {
        foreach (var (_, @event) in dict)
        {
            foreach (var target in @event.Targets)
            {
                switch (target.TargetSpecifier)
                {
                    case null:
                        if (!allowedNames.Contains(target.Target.Text))
                        {
                            Error.WriteError(target.SourceFile, target.Target.Source,
                                $"Unsupported name `{target.Target.Text}`. Expected: {string.Join(", ", allowedNames.Select(x => $"`{x}`"))}");
                        }
                        break;
                    case TypeSpecifier.Phase:
                        if (!data.Phases.ContainsKey(target.Target.Text))
                        {
                            Error.WriteError(target.SourceFile, target.Target.Source,
                                $"Phase `{target.Target.Text}` not found");
                        }
                        break;
                    case TypeSpecifier.Scene:
                        if (!data.Scenes.ContainsKey(target.Target.Text))
                        {
                            Error.WriteError(target.SourceFile, target.Target.Source,
                                $"Scene `{target.Target.Text}` not found");
                        }
                        break;
                }
            }
        }
    }

    private void CheckSteps(Dictionary<string, SequenceNode> sequences)
    {
        var context = new Context(data);
        context.Add("$game", Ast.ValueType.Mode);
        context.Add("$this", Ast.ValueType.Sequence);
        context.Add("$target", Ast.ValueType.Character);
        foreach (var (_, node) in sequences)
        {
            foreach (var step in node.Steps)
                HandleCodeBlock(step, step.Code, context, Ast.ValueType.Void);
        }
    }

    private void CheckSteps(Dictionary<string, EventNode> events)
    {
        var context = new Context(data);
        context.Add("$game", Ast.ValueType.Mode);
        context.Add("$this", Ast.ValueType.Sequence);
        foreach (var (_, @event) in events)
            foreach (var target in @event.Targets)
            {
                foreach (var step in target.Steps)
                    HandleCodeBlock(step, step.Code, context, Ast.ValueType.Void);
            }
    }

    private static void HandleCodeBlock(Ast.ISourceNode source, CodeBlock code, Context context, Ast.Type expected)
    {
        var ctx = new Context(context);
        var result = code.GetPreType(ctx);
        ctx.Check();
        Ast.TypeHelper.Check(source, result, expected);
        code.SetPostType(ctx, expected);
    }

    private void HandleModes(Dictionary<string, ModeNode> modes)
    {
        foreach (var (_, mode) in modes)
        {
            foreach (var character in mode.Character)
            {
                if (!data.Characters.ContainsKey(character.Text))
                {
                    Error.WriteError(character.SourceFile, character.Source, $"Character {character.Text} not found");
                }
            }
            foreach (var win in mode.Win)
            {
                if (!data.Wins.ContainsKey(win.Text))
                {
                    Error.WriteError(win.SourceFile, win.Source, $"Win {win.Text} not found");
                }
            }
        }
    }

    private sealed class Method(Validator validator, string name, Ast.Type type)
    {
        public string Name { get; } = name;

        public Ast.Type Type { get; } = type;

        public string? DocText { get; set; }

        public bool IsRequired { get; private set; }

        private readonly Context context = new(validator.data);

        public Dictionary<string, Func<object, bool>> Conditions { get; } = [];

        public Context GetContext<T>(T value)
            where T : notnull
        {
            if (Conditions.Count == 0)
                return context;
            var ctx = new Context(context);
            foreach (var (name, func) in Conditions)
                if (!func(value))
                    ctx.Delete(name);
            return ctx;
        }

        public Method Add(string name, Ast.ValueType type)
        {
            context.Add(name, type);
            return this;
        }

        public Method Add<T>(string name, Ast.ValueType type, Func<T, bool> condition)
        {
            context.Add(name, type);
            Conditions.Add(name, x => x is T value && condition(value));
            return this;
        }

        public Method Required()
        {
            IsRequired = true;
            return this;
        }

        public Method Doc(string docText)
        {
            DocText = docText;
            return this;
        }

        public void WriteDoc(Output output)
        {
            output.WriteLine($"### func `{Name}`");
            output.WriteLine();
            if (DocText is not null)
            {
                output.WriteLine(DocText);
                output.WriteLine();
            }
            output.WriteLine("| Info | Value |");
            output.WriteLine("|-|-|");
            output.Write($"| Return Type | ");
            Type.WriteDoc(output);
            output.WriteLine(" |");
            output.WriteLine($"| Required | {IsRequired} |");
            output.WriteLine();
            output.WriteLine("Variables:");
            output.WriteLine();
            if (context.Variables.Count == 0)
                output.WriteLine("- *none*");
            foreach (var (name, info) in context.Variables)
            {
                output.Write($"- `{name}`: ");
                info.Type.WriteDoc(output);
                output.WriteLine();
            }
            output.WriteLine();
        }
    }
}
