using LogicCompiler.Ast;

namespace LogicCompiler.Functions.AllFunctions;

internal sealed class Has : ICallFunction, IPipedFunction
{
    public string Name => "has";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count == 0)
        {
            Error.WriteError(name, $"At least one argument expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Ast.ValueType.Bool;
    }

    public Ast.Type GetPreType(Id name, Context context, Ast.Type consumedType, List<IExpression> Args)
    {
        return consumedType | Ast.ValueType.Collection;
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        if (Args.Count == 0)
            return;
        Args[0].SetPostType(context, Args[0].PreType.Flag & Ast.ValueType.GLOBAL_TYPES);
        var mask = Args[0].PreType.Flag.GetLabelMaskFromValueType();
        for (int i = 1; i < Args.Count; i++)
            Args[i].SetPostType(context, Ast.ValueType.LabelType | Ast.ValueType.ExplicitTypeInfo | mask);
    }

    public Ast.Type SetPostType(Id name, Context context, Ast.Type consumedType, List<IExpression> Args, Ast.Type expected)
    {
        Ast.TypeHelper.Check(name, consumedType,
            Ast.ValueType.Mode | Ast.ValueType.Character | Ast.ValueType.Scene |
            Ast.ValueType.Phase | Ast.ValueType.Voting | Ast.ValueType.Collection);
        var mask = consumedType.Flag.GetLabelMaskFromValueType();
        foreach (var arg in Args)
            arg.SetPostType(context, Ast.ValueType.LabelType | Ast.ValueType.ExplicitTypeInfo | mask);
        return consumedType | Ast.ValueType.Collection;
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        for (int i = 1; i < Args.Count; i++)
        {
            if (i > 1)
                output.Write(" || ");
            output.Write("(");
            Args[0].Write(output);
            output.Write(").Labels.Contains<");
            Args[i].Write(output);
            output.Write(">()");
        }
    }

    public void Write(Output output, Ast.Type consumedType, List<IExpression> Args, Ast.Type expected)
    {
        output.Write($".Where(_x => ");
        bool first = true;
        foreach (var arg in Args)
        {
            if (first)
                first = false;
            else output.Write(" || ");
            output.Write("_x.Labels.Contains<");
            arg.Write(output);
            output.Write(">()");
        }
        output.Write(")");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<item>, <label>, ...)",
            [
                ("<item>", Ast.ValueType.GLOBAL_TYPES),
                ("<label>", Ast.ValueType.LabelType),
                ("...", Ast.ValueType.LabelType),
            ],
            Ast.ValueType.Bool,
            """
            Returns true if `<item>` has at least one of the listed label set.
            """
        );
    }

    public void WritePipedDoc(Output output)
    {
        Registry.WritePipeDoc(
            output,
            $"{Name}(<label>, ...)",
            Ast.ValueType.GLOBAL_TYPES | Ast.ValueType.Collection,
            [
                ("<label>", Ast.ValueType.LabelType),
                ("...", Ast.ValueType.LabelType),
            ],
            Ast.ValueType.GLOBAL_TYPES | Ast.ValueType.Collection,
            """
            Filters the input collection for all elements that have at least one of the listed
            labels set. The returned collection type is identical to the input one.
            """
        );
    }
}

internal sealed class HasNot : ICallFunction, IPipedFunction
{
    public string Name => "has_not";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count == 0)
        {
            Error.WriteError(name, $"At least one argument expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Ast.ValueType.Bool;
    }

    public Ast.Type GetPreType(Id name, Context context, Ast.Type consumedType, List<IExpression> Args)
    {
        return consumedType | Ast.ValueType.Collection;
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        if (Args.Count == 0)
            return;
        Args[0].SetPostType(context, Args[0].PreType.Flag & Ast.ValueType.GLOBAL_TYPES);
        var mask = Args[0].PreType.Flag.GetLabelMaskFromValueType();
        for (int i = 1; i < Args.Count; i++)
            Args[i].SetPostType(context, Ast.ValueType.LabelType | Ast.ValueType.ExplicitTypeInfo | mask);
    }

    public Ast.Type SetPostType(Id name, Context context, Ast.Type consumedType, List<IExpression> Args, Ast.Type expected)
    {
        Ast.TypeHelper.Check(name, consumedType,
            Ast.ValueType.Mode | Ast.ValueType.Character | Ast.ValueType.Scene |
            Ast.ValueType.Phase | Ast.ValueType.Voting | Ast.ValueType.Collection);
        var mask = consumedType.Flag.GetLabelMaskFromValueType();
        foreach (var arg in Args)
            arg.SetPostType(context, Ast.ValueType.LabelType | Ast.ValueType.ExplicitTypeInfo | mask);
        return consumedType | Ast.ValueType.Collection;
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        for (int i = 1; i < Args.Count; i++)
        {
            if (i > 1)
                output.Write(" || ");
            output.Write("!(");
            Args[0].Write(output);
            output.Write(").Labels.Contains<");
            Args[i].Write(output);
            output.Write(">()");
        }
    }

    public void Write(Output output, Ast.Type consumedType, List<IExpression> Args, Ast.Type expected)
    {
        output.Write($".Where(_x => ");
        bool first = true;
        foreach (var arg in Args)
        {
            if (first)
                first = false;
            else output.Write(" || ");
            output.Write("!_x.Labels.Contains<");
            arg.Write(output);
            output.Write(">()");
        }
        output.Write(")");
    }
    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<item>, <label>, ...)",
            [
                ("<item>", Ast.ValueType.GLOBAL_TYPES),
                ("<label>", Ast.ValueType.LabelType),
                ("...", Ast.ValueType.LabelType),
            ],
            Ast.ValueType.Bool,
            """
            Returns true if `<item>` has at least one of the listed label is not set.
            """
        );
    }

    public void WritePipedDoc(Output output)
    {
        Registry.WritePipeDoc(
            output,
            $"{Name}(<label>, ...)",
            Ast.ValueType.GLOBAL_TYPES | Ast.ValueType.Collection,
            [
                ("<label>", Ast.ValueType.LabelType),
                ("...", Ast.ValueType.LabelType),
            ],
            Ast.ValueType.GLOBAL_TYPES | Ast.ValueType.Collection,
            """
            Filters the input collection for all elements that have at least one of the listed
            labels is not set. The returned collection type is identical to the input one.
            """
        );
    }
}

internal sealed class HasCharacter : ICallFunction, IPipedFunction
{
    public string Name => "has_character";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count == 0)
        {
            Error.WriteError(name, $"At least one argument expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Ast.ValueType.Bool;
    }

    public Ast.Type GetPreType(Id name, Context context, Ast.Type consumedType, List<IExpression> Args)
    {
        return Ast.ValueType.Character | Ast.ValueType.Collection;
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        if (Args.Count == 0)
            return;
        Args[0].SetPostType(context, Ast.ValueType.Character);
        for (int i = 1; i < Args.Count; i++)
            Args[i].SetPostType(context, Ast.ValueType.CharacterType | Ast.ValueType.ExplicitTypeInfo);
    }

    public Ast.Type SetPostType(Id name, Context context, Ast.Type consumedType, List<IExpression> Args, Ast.Type expected)
    {
        Ast.TypeHelper.Check(name, consumedType, Ast.ValueType.Character | Ast.ValueType.Scene | Ast.ValueType.Phase | Ast.ValueType.Voting | Ast.ValueType.Collection);
        foreach (var arg in Args)
            arg.SetPostType(context, Ast.ValueType.CharacterType | Ast.ValueType.ExplicitTypeInfo);
        return Ast.ValueType.Character | Ast.ValueType.Collection;
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        for (int i = 1; i < Args.Count; i++)
        {
            if (i > 1)
                output.Write(" || ");
            output.Write("(");
            Args[0].Write(output);
            output.Write(") is ");
            Args[i].Write(output);
        }
    }

    public void Write(Output output, Ast.Type consumedType, List<IExpression> Args, Ast.Type expected)
    {
        output.Write($".Where(_x => _x is ");
        bool first = true;
        foreach (var arg in Args)
        {
            if (first)
                first = false;
            else output.Write(" or ");
            arg.Write(output);
        }
        output.Write(")");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<character>, <type>, ...)",
            [
                ("<character>", Ast.ValueType.Character),
                ("<type>", Ast.ValueType.CharacterType),
                ("...", Ast.ValueType.CharacterType),
            ],
            Ast.ValueType.Bool,
            """
            Returns true if `<character>` is at least one of the listed character types.
            """
        );
    }

    public void WritePipedDoc(Output output)
    {
        Registry.WritePipeDoc(
            output,
            $"{Name}(<type>, ...)",
            Ast.ValueType.Character | Ast.ValueType.Collection,
            [
                ("<label>", Ast.ValueType.CharacterType),
                ("...", Ast.ValueType.CharacterType),
            ],
            Ast.ValueType.Character | Ast.ValueType.Collection,
            """
            Filters the input collection for all character that is at least one of the listed
            character types.
            """
        );
    }
}

internal sealed class HasNotCharacter : ICallFunction, IPipedFunction
{
    public string Name => "has_not_character";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count == 0)
        {
            Error.WriteError(name, $"At least one argument expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Ast.ValueType.Bool;
    }

    public Ast.Type GetPreType(Id name, Context context, Ast.Type consumedType, List<IExpression> Args)
    {
        return Ast.ValueType.Character | Ast.ValueType.Collection;
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        if (Args.Count == 0)
            return;
        Args[0].SetPostType(context, Ast.ValueType.Character);
        for (int i = 1; i < Args.Count; i++)
            Args[i].SetPostType(context, Ast.ValueType.CharacterType | Ast.ValueType.ExplicitTypeInfo);
    }

    public Ast.Type SetPostType(Id name, Context context, Ast.Type consumedType, List<IExpression> Args, Ast.Type expected)
    {
        Ast.TypeHelper.Check(name, consumedType, Ast.ValueType.Character | Ast.ValueType.Scene | Ast.ValueType.Phase | Ast.ValueType.Voting | Ast.ValueType.Collection);
        foreach (var arg in Args)
            arg.SetPostType(context, Ast.ValueType.CharacterType | Ast.ValueType.ExplicitTypeInfo);
        return Ast.ValueType.Character | Ast.ValueType.Collection;
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        for (int i = 1; i < Args.Count; i++)
        {
            if (i > 1)
                output.Write(" || ");
            output.Write("(");
            Args[0].Write(output);
            output.Write(") is not ");
            Args[i].Write(output);
        }
    }

    public void Write(Output output, Ast.Type consumedType, List<IExpression> Args, Ast.Type expected)
    {
        output.Write($".Where(_x => _x is ");
        bool first = true;
        foreach (var arg in Args)
        {
            if (first)
                first = false;
            else output.Write(" or ");
            output.Write(" not ");
            arg.Write(output);
        }
        output.Write(")");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<character>, <type>, ...)",
            [
                ("<character>", Ast.ValueType.Character),
                ("<type>", Ast.ValueType.CharacterType),
                ("...", Ast.ValueType.CharacterType),
            ],
            Ast.ValueType.Bool,
            """
            Returns true if `<character>` is not all of the listed character types.
            """
        );
    }

    public void WritePipedDoc(Output output)
    {
        Registry.WritePipeDoc(
            output,
            $"{Name}(<type>, ...)",
            Ast.ValueType.Character | Ast.ValueType.Collection,
            [
                ("<label>", Ast.ValueType.CharacterType),
                ("...", Ast.ValueType.CharacterType),
            ],
            Ast.ValueType.Character | Ast.ValueType.Collection,
            """
            Filters the input collection for all character that is not all of the listed character
            types.
            """
        );
    }
}

internal sealed class SetVisible : ICallFunction
{
    public string Name => "set_visible";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count != 2)
        {
            Error.WriteError(name, "Only two arguments expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Ast.ValueType.Void | Ast.ValueType.Mutable;
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, Ast.ValueType.Label | Ast.ValueType.TargetCharacter | Ast.ValueType.Character);
        if (Args.Count != 2)
            return;
        if (Args[1].PreType.CollectionDepth > 0)
            Args[1].SetPostType(context, Ast.ValueType.Character | Ast.ValueType.Collection);
        else
            Args[1].SetPostType(context, Ast.ValueType.Character);
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("Werewolf.Theme.Tools.MakeVisible(game, ");
        Args[0].Write(output);
        output.Write(", ");
        Args[1].Write(output);
        output.Write(")");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<target>, <characters>)",
            [
                ("<target>", Ast.ValueType.Label | Ast.ValueType.TargetCharacter | Ast.ValueType.Character),
                ("<character>", Ast.ValueType.Character | Ast.ValueType.Collection),
            ],
            Ast.ValueType.Void | Ast.ValueType.Mutable,
            """
            If `<target>` is a character label: Makes the `<target>` visible for all `<character>`.
            This ignores the default `view` function of the label.

            If `<target>` is a character: Makes the true identity of `<target>` visible for all
            `<character>`. This ignores the default `view` function of the character.

            There is a fast path if `<character>` is not a collection but a single character.
            """
        );
    }
}

internal sealed class SetInvisible : ICallFunction
{
    public string Name => "set_invisible";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count != 2)
        {
            Error.WriteError(name, "Only two arguments expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Ast.ValueType.Void | Ast.ValueType.Mutable;
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, Ast.ValueType.Label | Ast.ValueType.TargetCharacter | Ast.ValueType.Character);
        if (Args.Count != 2)
            return;
        if (Args[1].PreType.CollectionDepth > 0)
            Args[1].SetPostType(context, Ast.ValueType.Character | Ast.ValueType.Collection);
        else
            Args[1].SetPostType(context, Ast.ValueType.Character);
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("Werewolf.Theme.Tools.MakeInvisible(game, ");
        Args[0].Write(output);
        output.Write(", ");
        Args[1].Write(output);
        output.Write(")");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<target>, <characters>)",
            [
                ("<target>", Ast.ValueType.Label | Ast.ValueType.TargetCharacter | Ast.ValueType.Character),
                ("<character>", Ast.ValueType.Character | Ast.ValueType.Collection),
            ],
            Ast.ValueType.Void | Ast.ValueType.Mutable,
            """
            If `<target>` is a character label: Makes the `<target>` no longer visible for all
            `<character>`. It can still be visible due to the `view` function of the label.

            If `<target>` is a character: Makes the true identity of `<target>` no longer visible
            for all `<character>`.It can still be visible due to the `view` function of the
            character.

            There is a fast path if `<character>` is not a collection but a single character.
            """
        );
    }
}

internal sealed class Filter : IPipedFunction, ICustomArgumentHandler
{
    public string Name => "filter";

    public Ast.Type GetPreType(Id name, Context context, Ast.Type consumedType, List<IExpression> Args)
    {
        if (Args.Count != 2)
        {
            Error.WriteError(name, "two arguments expected");
            return Ast.ValueType.Void;
        }
        if (!consumedType.HasFlag(Ast.ValueType.Collection))
        {
            Error.WriteError(name, "Expect a collection to be piped in");
            return Ast.ValueType.Void;
        }
        var varName = Tools.ExpectVariable(context, Args[0]);
        if (varName is null)
            return Ast.ValueType.Void;
        var argContext = new Context(context);
        argContext.Add(varName, consumedType.RemoveCollection());
        _ = Args[1].GetPreType(argContext);
        return consumedType.EnforceCollection();
    }

    public Ast.Type SetPostType(Id name, Context context, Ast.Type consumedType, List<IExpression> Args, Ast.Type expected)
    {
        var varName = Tools.ExpectVariable(context, Args[0]);
        if (varName is null)
            return Ast.ValueType.Void;
        var argContext = new Context(context);
        argContext.Add(varName, expected.RemoveCollection());
        Args.At(1)?.SetPostType(argContext, Ast.ValueType.Bool);
        return expected;
    }

    public void Write(Output output, Ast.Type consumedType, List<IExpression> Args, Ast.Type expected)
    {
        output.Write(".Where(");
        Args[0].Write(output);
        output.Write(" =>");
        output.WriteLine();
        output.WriteBlockBegin();
        output.Write("return ");
        Args[1].Write(output);
        output.WriteLine(";");
        output.Pop();
        output.Write("})");
    }

    public void WritePipedDoc(Output output)
    {
        Registry.WritePipeDoc(
            output,
            $"{Name}(<name>, <action>)",
            Ast.ValueType.None | Ast.ValueType.Collection,
            [
                ("<name>", Ast.ValueType.None),
                ("<action>", Ast.ValueType.Bool),
            ],
            Ast.ValueType.None | Ast.ValueType.Collection,
            """
            Filters the input for all elements for that `<action>` returns true. `<name>` is the
            name of the variable that is only available inside `<action>`.

            ```
            let $active = $labels | filter($lbl,
                if let $value = get_with($lbl, label MyLabel, counter) {
                    $value >= 2
                } else {
                    false
                }
            )
            ```
            """
        );
    }
}

internal sealed class Labels : ICallFunction
{
    public string Name => "labels";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count != 1)
        {
            Error.WriteError(name, "one argument expected");
            return Ast.ValueType.Void;
        }
        if (Args[0].Name is not null)
            Error.WriteError(Args[0], $"Argument is not allowed to have a name");
        return Ast.ValueType.Label | Ast.ValueType.Collection |
            TypeHelper.GetLabelMaskFromValueType(Args[0].PreType.Flag);
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        var allowed = Ast.ValueType.Mode | Ast.ValueType.Character | Ast.ValueType.Scene |
            Ast.ValueType.Phase | Ast.ValueType.Voting;

        Args.At(0)?.SetPostType(context, allowed & Args.At(0)?.PreType ?? allowed);
        if (Args.Count > 0)
            Ast.TypeHelper.CheckIfOnlyOneSet(Args[0], Args[0].PreType.Flag, allowed);
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("(");
        Args[0].Write(output);
        output.Write(").Labels");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<target>)",
            [
                ("<target>", Ast.ValueType.Mode | Ast.ValueType.Character | Ast.ValueType.Scene |
                    Ast.ValueType.Phase | Ast.ValueType.Voting),
            ],
            Ast.ValueType.Label | Ast.ValueType.Collection,
            """
            Returns all stored label to `<target>`.
            """
        );
    }
}

internal sealed class Add : ICallFunction
{
    public string Name => "add";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count < 2)
        {
            Error.WriteError(name, "two or more arguments expected");
            return Ast.ValueType.Void;
        }
        for (int i = 0; i < Args.Count; ++i)
        {
            if (i < 2 && Args[i].Name is not null)
                Error.WriteError(Args[i], $"Argument is not allowed to have a name");
            if (i >= 2 && Args[i].Name is null)
                Error.WriteError(Args[i], $"Argument must have a name that is associated with the label");
        }
        return Ast.ValueType.Label | Ast.ValueType.Mutable | (Args[0].PreType.Flag & Ast.ValueType.LABEL_TARGET);
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        var allowed = Ast.ValueType.Mode | Ast.ValueType.Character | Ast.ValueType.Scene |
            Ast.ValueType.Phase | Ast.ValueType.Voting;

        Args.At(0)?.SetPostType(context, allowed & Args.At(0)?.PreType ?? allowed);
        if (Args.Count > 0)
            Ast.TypeHelper.CheckIfOnlyOneSet(Args[0], Args[0].PreType.Flag, allowed);

        var type = Ast.ValueType.LabelType | Ast.ValueType.ExplicitTypeInfo |
            (Args.Count == 0 ? Ast.ValueType.None : TypeHelper.GetLabelMaskFromValueType(Args[0].PreType.Flag));
        Args.At(1)?.SetPostType(context, type);
        var label = Args.Count < 2 ? null : Tools.ExpectArgAsLabel(context, Args[1]);
        if (label is not null)
        {
            var defined = new HashSet<string>();
            for (int i = 2; i < Args.Count; ++i)
            {
                if (Args[i].Name is null)
                    continue;
                var with = label.GetWith(Args[i].Name!.Text);
                if (with is null)
                {
                    Error.WriteError(Args[i].Name!, $"Name {Args[i].Name} not defined for label {label.Name.Text}");
                    continue;
                }
                Args[i].SetPostType(context, with.GetValueType());
                if (!defined.Add(Args[i].Name!.Text))
                {
                    Error.WriteError(Args[i].Name!, $"Cannot redefine {Args[i].Name}");
                    continue;
                }
            }
            var missing = label.Withs.Select(x => x.Name.Text)
                .Where(x => !defined.Contains(x))
                .ToList();
            if (missing.Count > 0)
                Error.WriteError(name, $"Not all member of label {label.Name.Text} are defined. Missing: {string.Join(", ", missing)}");
        }
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("Werewolf.Theme.Tools.Add(");
        if (!Args[0].PreType.HasFlag(Ast.ValueType.Mode))
            output.Write("game, ");
        Args[0].Write(output);
        output.Write(", new ");
        Args[1].Write(output);
        if (Args.Count > 2)
        {
            output.WriteLine("(");
            output.Push();
            for (int i = 2; i < Args.Count; ++i)
            {
                output.Write($"{Args[i].Name?.Text}: ");
                Args[i].Write(output);
                if (i + 1 < Args.Count)
                    output.WriteLine(",");
                else output.WriteLine();
            }
            output.Pop();
            output.Write("))");
        }
        else output.Write("())");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<target>, <label>, <args...>)",
            [
                ("<target>", Ast.ValueType.Mode | Ast.ValueType.Character | Ast.ValueType.Scene |
                    Ast.ValueType.Phase | Ast.ValueType.Voting),
                ("<label>", Ast.ValueType.Label),
                ("<args...>", Ast.ValueType.None),
            ],
            Ast.ValueType.Label | Ast.ValueType.Mutable,
            """
            Create a new `<label>`, add it to `<target>` and return it for further usage.
            `<args...>` is the list of all named arguments that are used to define additional meta
            data of the new label. All meta data fields have to be defined at creation!

            ```
            label MyLabel {
                with int num;
                with string name;
                // ...
            }
            // ...
            let $newLabel = add($target, label MyLabel, num=1, name="name");
            ```
            """
        );
    }
}

internal sealed class GetWith : ICallFunction, ICustomArgumentHandler
{
    public string Name => "get_with";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count != 3)
        {
            Error.WriteError(name, "three arguments expected");
            return Ast.ValueType.Void;
        }
        _ = Args[0].GetPreType(context);
        _ = Args[1].GetPreType(context);
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg, $"Argument is not allowed to have a name");
        var label = Tools.ExpectArgAsLabel(context, Args[1]);
        var fieldName = Tools.ExpectName(context, Args[2]);
        if (label is null || fieldName is null)
        {
            return Ast.ValueType.Void;
        }
        var field = label.GetWith(fieldName);
        if (field is null)
        {
            Error.WriteError(Args[2], $"Field `{fieldName}` is not defined for the label `{label.Name.Text}`.");
            return Ast.ValueType.Void;
        }
        return field.GetValueType() | Ast.ValueType.Optional;
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, Ast.ValueType.Label);
        Args.At(1)?.SetPostType(context, Ast.ValueType.LabelType | Ast.ValueType.ExplicitTypeInfo);
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("Werewolf.Theme.Tools.GetField<");
        Args[1].Write(output);
        output.Write(", ");
        (expected & ~Ast.ValueType.Optional).Write(output);
        output.Write(">(");
        Args[0].Write(output);
        output.Write(", _x => _x.Member_");
        output.Write((Args[2].Value as Ast.IdExpression)?.Name.Text ?? "");
        output.Write(")");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<label>, <type>, <field>)",
            [
                ("<label>", Ast.ValueType.Label),
                ("<type>", Ast.ValueType.LabelType),
                ("<field>", Ast.ValueType.None),
            ],
            Ast.ValueType.None | Ast.ValueType.Optional,
            """
            Access the `<field>` of the specified `<label>` instance, which was set when adding the
            label to its target. If the `<label>` is not of the given `<type>`, this will return
            just an empty value which can be handled using an if-let statement.

            ```
            label MyLabel {
                with string Name;
                //...
            ```

            ```
            if let $name = get_with($label, label MyLabel, Name) {
                notify $name
            }
            ```
            """
        );
    }
}

internal sealed class Remove : ICallFunction
{
    public string Name => "remove";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count != 2)
        {
            Error.WriteError(name, "Only two arguments expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Ast.ValueType.Void | Ast.ValueType.Mutable;
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        var allowed = Ast.ValueType.Mode | Ast.ValueType.Character | Ast.ValueType.Scene |
            Ast.ValueType.Phase | Ast.ValueType.Voting;
        Args.At(0)?.SetPostType(context, allowed & Args.At(0)?.PreType ?? allowed);
        if (Args.Count > 0)
            Ast.TypeHelper.CheckIfOnlyOneSet(Args[0], Args[0].PreType.Flag, allowed);

        if (Args.Count != 2)
            return;
        var type = TypeHelper.GetLabelMaskFromValueType(Args[0].PreType.Flag);
        if (Args[1].PreType.HasFlag(Ast.ValueType.LabelType | Ast.ValueType.Label))
            type |= Ast.ValueType.LabelType | Ast.ValueType.Label;
        else if (Args[1].PreType.HasFlag(Ast.ValueType.LabelType))
            type |= Ast.ValueType.LabelType | Ast.ValueType.ExplicitTypeInfo;
        else if (Args[1].PreType.HasFlag(Ast.ValueType.Label))
            type |= Ast.ValueType.Label;
        else type |= Ast.ValueType.LabelType | Ast.ValueType.Label;
        Args.At(1)?.SetPostType(context, type);

    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        if (Args[1].PostType.HasFlag(Ast.ValueType.ExplicitTypeInfo))
        {
            output.Write("Werewolf.Theme.Tools.Remove<");
            Args[1].Write(output);
            output.Write(">(");
            if (!Args[0].PreType.HasFlag(Ast.ValueType.Mode))
                output.Write("game, ");
            Args[0].Write(output);
            output.Write(")");
        }
        else
        {
            output.Write("Werewolf.Theme.Tools.Remove(");
            if (!Args[0].PreType.HasFlag(Ast.ValueType.Mode))
                output.Write("game, ");
            Args[0].Write(output);
            output.Write(", ");
            Args[1].Write(output);
            output.Write(")");
        }
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<target>, <label>)",
            [
                ("<target>", Ast.ValueType.Mode | Ast.ValueType.Character | Ast.ValueType.Scene |
                    Ast.ValueType.Phase | Ast.ValueType.Voting),
                ("<label>", Ast.ValueType.Label),
            ],
            Ast.ValueType.Void | Ast.ValueType.Mutable,
            """
            Remove all `<label>` to all `<target>`.
            """
        );
    }
}

internal sealed class Empty : ICallFunction
{
    public string Name => "empty";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count != 1)
        {
            Error.WriteError(name, "Only one argument expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Ast.ValueType.Bool;
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, Args[0].PreType.EnforceCollection());
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("!(");
        Args[0].Write(output);
        output.Write(").Any()");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<col>)",
            [
                ("<col>", Ast.ValueType.Collection),
            ],
            Ast.ValueType.Bool,
            """
            Returns true if `<col>` is empty.
            """
        );
    }
}

internal sealed class Any : ICallFunction
{
    public string Name => "any";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count != 1)
        {
            Error.WriteError(name, "Only one argument expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Ast.ValueType.Bool;
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, Args[0].PreType.EnforceCollection());
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        Args[0].Write(output);
        output.Push();
        output.WriteLine(".Any()");
        output.Pop();
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<col>)",
            [
                ("<col>", Ast.ValueType.Collection),
            ],
            Ast.ValueType.Bool,
            """
            Returns true if `<col>` contains at least one element.
            """
        );
    }
}

internal sealed class Length : ICallFunction
{
    public string Name => "length";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count != 1)
        {
            Error.WriteError(name, "Only one argument expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Ast.ValueType.Int;
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, Args[0].PreType.EnforceCollection());
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("(");
        Args[0].Write(output);
        output.Write(").Count()");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<col>)",
            [
                ("<col>", Ast.ValueType.Collection),
            ],
            Ast.ValueType.Int,
            """
            Returns the length of `<col>`.
            """
        );
    }
}

internal sealed class Rand : ICallFunction
{
    public string Name => "rand";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count != 1)
        {
            Error.WriteError(name, "Only one argument expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Ast.ValueType.Int;
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, Ast.ValueType.Int);
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("Werewolf.Theme.Tools.GetRandom(");
        Args[0].Write(output);
        output.Write(")");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<max>)",
            [
                ("<max>", Ast.ValueType.Int),
            ],
            Ast.ValueType.Int,
            """
            Returns a random number between 0 and `<max>`-1.
            """
        );
    }
}
internal sealed class Get : ICallFunction, IPipedFunction
{
    public string Name => "get";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count != 2)
        {
            Error.WriteError(name, "Only two arguments expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Args[1].PreType.RemoveCollection() | Ast.ValueType.Optional;
    }

    public Ast.Type GetPreType(Id name, Context context, Ast.Type consumedType, List<IExpression> Args)
    {
        if (Args.Count != 1)
        {
            Error.WriteError(name, "Only one argument expected");
            return Ast.ValueType.Void;
        }
        return consumedType.RemoveCollection().MakeOptional();
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, Ast.ValueType.Int);
        Args.At(1)?.SetPostType(context, Args[1].PreType.EnforceCollection().RemoveOptional());
    }

    public Ast.Type SetPostType(Id name, Context context, Ast.Type consumedType, List<IExpression> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, Ast.ValueType.Int);
        return consumedType.EnforceCollection().RemoveOptional();
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("Werewolf.Theme.Tools.Get(");
        Args[1].Write(output);
        output.Write(", ");
        Args[0].Write(output);
        output.Write(")");
    }

    public void Write(Output output, Ast.Type consumedType, List<IExpression> Args, Ast.Type expected)
    {
        output.Write(".Get(");
        Args[0].Write(output);
        output.Write(")");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<index>, <col>)",
            [
                ("<index>", Ast.ValueType.Int),
                ("<col>", Ast.ValueType.Collection),
            ],
            Ast.ValueType.Optional,
            """
            Returns the `<index>` element from the collection `<col>`.
            """
        );
    }

    public void WritePipedDoc(Output output)
    {
        Registry.WritePipeDoc(
            output,
            $"{Name}(<index>)",
            Ast.ValueType.Collection,
            [
                ("<index>", Ast.ValueType.Int),
            ],
            Ast.ValueType.Optional,
            """
            Returns the `<index>` element from the piped collection.
            """
        );
    }
}


internal sealed class Get2 : ICallFunction
{
    public string Name => "get2";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count != 2)
        {
            Error.WriteError(name, "Only two arguments expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Args[1].PreType.RemoveCollection();
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, Ast.ValueType.Int);
        Args.At(1)?.SetPostType(context, Args[1].PreType.EnforceCollection(2));
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("Werewolf.Theme.Tools.GetCol(");
        Args[0].Write(output);
        output.Write(", ");
        Args[1].Write(output);
        output.Write(")");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<index>, <col>)",
            [
                ("<index>", Ast.ValueType.Int),
                ("<col>", new(Ast.ValueType.Collection, 2)),
            ],
            Ast.ValueType.None,
            """
            Returns the `<index>` collection from the nested collection `<col>`. If `<index>`
            doesn't exist in `<col>` an empty collection will be returned.
            """
        );
    }
}

internal sealed class Shuffle : ICallFunction
{
    public string Name => "shuffle";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count != 1)
        {
            Error.WriteError(name, "Only one argument expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Args[0].PreType.EnforceCollection();
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, Args[0].PreType.EnforceCollection());
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("Werewolf.Theme.Tools.Shuffle(");
        Args[0].Write(output);
        output.Write(")");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<col>)",
            [
                ("<col>", Ast.ValueType.Collection),
            ],
            Ast.ValueType.Collection,
            """
            Shuffles all entries in `<col>` and returns a collection with all entries in random
            order.
            """
        );
    }
}

internal sealed class Split : ICallFunction
{
    public string Name => "split";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count != 2)
        {
            Error.WriteError(name, "Only two arguments expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Args[1].PreType.AddCollection();
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, Ast.ValueType.Int);
        Args.At(1)?.SetPostType(context, Args[1].PreType.EnforceCollection());
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("Werewolf.Theme.Tools.Split(");
        Args[0].Write(output);
        output.Write(", ");
        Args[1].Write(output);
        output.Write(")");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<count>, <col>)",
            [
                ("<count>", Ast.ValueType.Int),
                ("<col>", Ast.ValueType.Collection),
            ],
            new(Ast.ValueType.Collection, 2),
            $"""
            Split `<col>` in `<count>` collections and return them in a nested collection. It tries
            to spread the elements evenly. If `<count>` is larger than the number of entries in
            `<col>`, the last elements in the returned collection will be empty. If `<count>` is
            smaller or equal zero, it will behave like you have specified `<count>`=1.

            You can only split up to {int.MaxValue} groups!
            """
        );
    }
}

internal sealed class Enabled : ICallFunction
{
    public string Name => "enabled";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count is 0 or > 2)
        {
            Error.WriteError(name, "Only one or two arguments expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Args.Count == 2 ? Ast.ValueType.Bool | Ast.ValueType.Mutable : Ast.ValueType.Bool;
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, Ast.ValueType.Character);
        Args.At(1)?.SetPostType(context, Ast.ValueType.Bool);
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("(");
        Args[0].Write(output);
        output.Write(").Enabled");
        if (Args.Count == 2)
        {
            output.Write(" = ");
            Args[1].Write(output);
        }
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<character>, <state>)",
            [
                ("<character>", Ast.ValueType.Character),
                ("<state>", Ast.ValueType.Bool),
            ],
            Ast.ValueType.Mutable | Ast.ValueType.Bool,
            $"""
            Enable or disable the specified character. The argument `<state>` is optional and if
            omitted the call will just return the current value and doesn't mutate any state.

            Disabling a character removes it from the collection `@character` and enabling it adds
            it to the collection `@character` again. The collection `@all_character` is unchanged.

            The enable state is similar to a label but it is respected by the core systems.
            """
        );
    }
}

internal sealed class Flatten : ICallFunction, IPipedFunction
{
    public string Name => "flatten";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count is not 1)
        {
            Error.WriteError(name, "Only one argument expected");
            return Ast.ValueType.Void;
        }
        foreach (var arg in Args)
            if (arg.Name is not null)
                Error.WriteError(arg.Name, $"Argument is not allowed to have a name");
        return Args[0].PreType.RemoveCollection().EnforceCollection();
    }

    public Ast.Type GetPreType(Id name, Context context, Ast.Type consumedType, List<IExpression> Args)
    {
        if (Args.Count != 0)
        {
            Error.WriteError(name, "No arguments expected");
            return Ast.ValueType.Void;
        }
        return consumedType.RemoveCollection().EnforceCollection();
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, expected.AddCollection());
    }

    public Ast.Type SetPostType(Id name, Context context, Ast.Type consumedType, List<IExpression> Args, Ast.Type expected)
    {
        return expected.AddCollection();
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("(");
        Args[0].Write(output);
        output.Write(").SelectMany(_x => _x)");
    }

    public void Write(Output output, Ast.Type consumedType, List<IExpression> Args, Ast.Type expected)
    {
        output.Write(".SelectMany(_x => _x)");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<collection>)",
            [
                ("<collection>", new Ast.Type(Ast.ValueType.Collection, 2)),
            ],
            new Ast.Type(Ast.ValueType.Collection, 1),
            $"""
            Flattens the `<collection>` by concatting all contained lists.
            """
        );
    }

    public void WritePipedDoc(Output output)
    {
        Registry.WritePipeDoc(
            output,
            $"{Name}()",
            new Ast.Type(Ast.ValueType.Collection, 2),
            [],
            new Ast.Type(Ast.ValueType.Collection, 1),
            $"""
            Flattens the piped collection by concatting all contained lists.
            """
        );
    }
}

internal sealed class Cancel : ICallFunction
{
    public string Name => "cancel";

    public Ast.Type GetPreType(Id name, Context context, List<Argument> Args)
    {
        if (Args.Count != 1)
        {
            Error.WriteError(name, "one argument expected");
            return Ast.ValueType.Void;
        }
        if (Args[0].Name is not null)
            Error.WriteError(Args[0], $"Argument is not allowed to have a name");
        return Ast.ValueType.Void | Ast.ValueType.Mutable;
    }

    public void SetPostType(Id name, Context context, List<Argument> Args, Ast.Type expected)
    {
        Args.At(0)?.SetPostType(context, Ast.ValueType.Voting);
    }

    public void Write(Output output, List<Argument> Args, Ast.Type expected)
    {
        output.Write("Werewolf.Theme.Tools.Cancel(game, ");
        Args[0].Write(output);
        output.Write(")");
    }

    public void WriteCallDoc(Output output)
    {
        Registry.WriteCallDoc(
            output,
            $"{Name}(<voting>)",
            [
                ("<voting>", Ast.ValueType.Voting),
            ],
            Ast.ValueType.Void | Ast.ValueType.Mutable,
            """
            Cancels the referenced `<voting>` instance and removes it from the game. There is no
            voting result handler called.

            If you cancel all existing votings and the game rule `AutoFinishRounds` is active, the
            next scene will be started.

            There is no need to cancel a voting in its result handler, because the voting is
            automatically removed after finishing this handler.
            """
        );
    }
}
