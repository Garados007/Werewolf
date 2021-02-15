using System;
using System.Collections.Generic;
using OneOf;

namespace Werewolf.Theme
{
    public class PhaseFlowBuilder
    {
        readonly List<OneOf<Stage, Phase, PhaseFlow.PhaseGroup>> phases = new List<OneOf<Stage, Phase, PhaseFlow.PhaseGroup>>();

        public void Add(Phase phase)
        {
            phases.Add(phase);
        }

        public void Add(IEnumerable<Phase> phases)
        {
            foreach (var phase in phases)
                this.phases.Add(phase);
        }

        public void Add(Func<IEnumerable<Phase>> phases)
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
            if (group == null)
                return null;
            else return new PhaseFlow(group.Entry);
        }

        public PhaseFlow.PhaseGroup? BuildGroup()
        {
            if (phases.Count == 0)
                return null;

            PhaseFlow.Step? last = null, init = null;
            Stage? stage = null;
            foreach (var stagePhase in phases)
            {
                if (stagePhase.TryPickT0(out Stage stage_, out OneOf<Phase, PhaseFlow.PhaseGroup> phaseOrPhaseGroup))
                {
                    stage = stage_;
                    continue;
                }
                if (stage == null)
                    return null;
                PhaseFlow.Step step;
                if (phaseOrPhaseGroup.TryPickT0(out Phase phase, out PhaseFlow.PhaseGroup group))
                    step = new PhaseFlow.Step(stage, phase);
                else step = new PhaseFlow.Step(stage, group);

                if (last != null)
                    last.Next = step;
                last = step;
                init ??= step;
            }

            if (init != null)
                return new PhaseFlow.PhaseGroup(init);
            else return null;
        }
    }
}