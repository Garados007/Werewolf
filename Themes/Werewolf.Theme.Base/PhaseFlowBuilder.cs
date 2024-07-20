using OneOf;

namespace Werewolf.Theme;

public class PhaseFlowBuilder
{
    private readonly List<OneOf<Phase, Scene, PhaseFlow.PhaseGroup>> phases
        = [];

    public void Add(Scene scene)
    {
        phases.Add(scene);
    }

    public void Add(IEnumerable<Scene> scenes)
    {
        foreach (var scene in scenes)
            this.phases.Add(scene);
    }

    public void Add(Func<IEnumerable<Scene>> scenes)
    {
        Add(scenes());
    }

    public void Add(Phase phase)
    {
        phases.Add(phase);
    }

    public void Add(PhaseFlow.PhaseGroup group)
    {
        phases.Add(group);
    }

    public PhaseFlow? BuildPhaseFlow()
    {
        var group = BuildGroup();
        return group == null ? null : new PhaseFlow(group.Entry);
    }

    public PhaseFlow.PhaseGroup? BuildGroup()
    {
        if (phases.Count == 0)
            return null;

        PhaseFlow.Step? last = null, init = null;
        Phase? stage = null;
        foreach (var stagePhase in phases)
        {
            if (stagePhase.TryPickT0(out Phase stage_, out OneOf<Scene, PhaseFlow.PhaseGroup> phaseOrPhaseGroup))
            {
                stage = stage_;
                continue;
            }
            if (stage == null)
                return null;
            var step = phaseOrPhaseGroup.TryPickT0(out Scene phase, out PhaseFlow.PhaseGroup group)
                ? new PhaseFlow.Step(stage, phase)
                : new PhaseFlow.Step(stage, group);
            if (last != null)
                last.Next = step;
            last = step;
            init ??= step;
        }

        return init != null ? new PhaseFlow.PhaseGroup(init) : null;
    }
}
