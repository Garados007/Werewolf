namespace LogicCompiler.Ast;

internal sealed class Flow
{
    private readonly Dictionary<string, HashSet<string>> nextScenes = [];

    private readonly HashSet<string> roots = [];

    private readonly Dictionary<string, HashSet<string>> scenesByPhase = [];

    public Flow(Generator generator)
    {
        AddAll(generator);
        CheckCircles(generator);
    }

    private void AddAll(Generator generator)
    {
        foreach (var (name, _) in generator.Scenes)
        {
            nextScenes.Add(name, []);
            _ = roots.Add(name);
        }
        foreach (var (_, scene) in generator.Scenes)
        {
            if (scene.Phase is null)
            {
                Error.WriteError(scene.Name, $"No phase associated for {scene.Name.Text}");
                continue;
            }
            if (!scenesByPhase.TryGetValue(scene.Phase.Text, out var sceneSet))
                scenesByPhase.Add(scene.Phase.Text, sceneSet = []);
            _ = sceneSet.Add(scene.Name.Text);
            foreach (var source in scene.After)
                Link(generator, source, source, scene.Name);
            foreach (var target in scene.Before)
                Link(generator, target, scene.Name, target);
        }
    }

    private void Link(Generator generator, Id refName, Id source, Id target)
    {
        if (!generator.Scenes.TryGetValue(source.Text, out var sourceScene))
        {
            Error.WriteError(source, $"Scene {source.Text} is not defined");
            return;
        }
        if (!generator.Scenes.TryGetValue(target.Text, out var targetScene))
        {
            Error.WriteError(target, $"Scene {target.Text} is not defined");
            return;
        }
        if (sourceScene.Phase is null || targetScene.Phase is null)
        {
            // already reported
            return;
        }
        if (sourceScene.Phase!.Text != targetScene.Phase!.Text)
        {
            Error.WriteError(refName, $"Invalid referenced phases. {source.Text} references to {sourceScene.Phase!.Text} and {target.Text} references to {targetScene.Phase!.Text}.");
            return;
        }
        _ = roots.Remove(target.Text);
        // there is no issue if this link is already defined. Just ignore it.
        _ = nextScenes[source.Text].Add(target.Text);
    }

    private void CheckCircles(Generator generator)
    {
        var allMarked = new HashSet<string>();
        foreach (var root in roots)
        {
            var jobs = new Stack<(string name, bool start)>();
            var nextId = 0;
            var ids = new Dictionary<string, (int open, int? close)>();
            jobs.Push((root, true));
            while (jobs.TryPop(out var job))
            {
                if (job.start)
                {
                    _ = allMarked.Add(job.name);
                    if (ids.TryGetValue(job.name, out var refs))
                    {
                        if (refs.close.HasValue)
                            continue;
                        var scene = generator.Scenes[job.name];
                        var path = ids.Where(x => !x.Value.close.HasValue)
                            .Where(x => x.Value.open >= refs.open)
                            .OrderBy(x => x.Value.open)
                            .Select(x => x.Key);
                        Error.WriteError(scene.Name, $"Reference circle at scene {job.name} found: {string.Join(" -> ", path)} -> {job.name}");
                        continue;
                    }
                    ids.Add(job.name, (nextId++, null));
                    jobs.Push((job.name, false));
                    foreach (var next in nextScenes[job.name])
                        jobs.Push((next, true));
                }
                else
                {
                    // close the reference
                    if (ids.TryGetValue(job.name, out var refs) && !refs.close.HasValue)
                        ids[job.name] = (refs.open, nextId++);
                }
            }
        }
        foreach (var (name, _) in nextScenes)
            if (!allMarked.Contains(name))
            {
                var scene = generator.Scenes[name];
                Error.WriteError(scene.Name, $"Scene {name} is not reachable from any root scene and therefore part of a cycle!");
            }
    }

    /// <summary>
    /// Get the linearized scene order for the given phase. Attention: If the scenes contain any
    /// circle, this function will never halt and produce a result!
    /// </summary>
    /// <param name="phase">The name of the phase</param>
    /// <returns>the linearized scene order</returns>
    public List<string> GetSceneOrder(string phase)
    {
        if (!scenesByPhase.TryGetValue(phase, out var scenes))
            return [];
        var id = 0;
        var weights = new Dictionary<string, int>();
        var jobs = new Queue<string>();
        foreach (var scene in scenes)
            jobs.Enqueue(scene);
        // This loop does only terminate if we have NO circles!
        //
        // Proof:
        //   1. jobs contain all scenes in the current phase and have to be looked at
        //   2. All root scenes if no other scenes in jobs pointing to it are eliminated at the
        //      first check and not readded again (otherwise they wouldn't be a root node).
        //   3. All other scenes are given a higher id and checked again at a future point in time
        //   4. The combination of 2 and 3 results in: Every root node is removed and their next
        //      nodes can become new root nodes and will be removed in the next iteration. This is
        //      done n times (at worst the number of jobs) but ultimately it will be removed,
        //      because they are no circles.
        //   5. At some point in time the job list is empty and the algorithm stops.
        //
        // This algorithm is not the most efficient but the number of elements are very small and
        // this is done ahead in time. The generated code will contain only the result.
        while (jobs.TryDequeue(out var current))
        {
            weights[current] = id++;
            foreach (var next in nextScenes[current])
                jobs.Enqueue(next);
        }
        return weights.OrderBy(x => x.Value).Select(x => x.Key).ToList();
    }
}
