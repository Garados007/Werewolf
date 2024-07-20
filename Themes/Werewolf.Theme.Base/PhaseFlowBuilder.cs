using OneOf;

namespace Werewolf.Theme;

public class PhaseFlowBuilder
{
    private readonly List<OneOf<Stage, Scene, PhaseFlow.PhaseGroup>> phases
        = new List<OneOf<Stage, Scene, PhaseFlow.PhaseGroup>>();

    public void Add(Scene phase)
    {
        phases.Add(phase);
    }

    public void Add(IEnumerable<Scene> phases)
    {
        foreach (var phase in phases)
            this.phases.Add(phase);
    }

    public void Add(Func<IEnumerable<Scene>> phases)
    {
        Add(phases());
    }

    public void Add(Stage stage)
    {
        phases.Add(stage);
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
        Stage? stage = null;
        foreach (var stagePhase in phases)
        {
            if (stagePhase.TryPickT0(out Stage stage_, out OneOf<Scene, PhaseFlow.PhaseGroup> phaseOrPhaseGroup))
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
