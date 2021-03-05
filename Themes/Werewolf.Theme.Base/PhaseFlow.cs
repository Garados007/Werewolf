using OneOf;
using System.Linq;
using System.Threading.Tasks;

namespace Werewolf.Theme
{
    public sealed class PhaseFlow
    {
        /// <summary>
        /// this phase is meant to be skipped durring startup
        /// </summary>
        private sealed class InitialPhase : Phase
        {
            public override bool IsGamePhase => false;

            public override bool CanExecute(GameRoom game)
            {
                return true;
            }

            public override bool CanMessage(GameRoom game, Role role)
            {
                return false;
            }
        }

        private sealed class InitialStage : Stage
        {
            public override string LanguageId => "";

            public override string BackgroundId => "";

            public override string ColorTheme => "";
        }

        public sealed class Step
        {
            public OneOf<Phase, PhaseGroup> Phase { get; }

            public Stage Stage { get; }

            public Step? Next { get; internal set; }

            public Step CurrentStep => Phase.TryPickT0(out _, out PhaseGroup phaseGroup) ? this : phaseGroup.Current.CurrentStep;

            public Phase CurrentPhase => Phase.Match(x => x, x => x.Current.CurrentPhase);

            internal Step(Stage stage, Phase phase)
                => (Stage, Phase) = (stage, phase);

            internal Step(Stage stage, PhaseGroup group)
                => (Stage, Phase) = (stage, group);

            public void SetExecute()
            {
                if (Phase.TryPickT1(out PhaseGroup phaseGroup, out _))
                {
                    phaseGroup.IsExecuted = true;
                    phaseGroup.Current.SetExecute();
                }
            }
        }

        public sealed class PhaseGroup
        {
            public Step Entry { get; }

            public Step Current { get; private set; }

            public bool IsExecuted { get; internal set; }

            public bool IsEntered { get; internal set; }

            public PhaseGroup(Step entry)
                => (Entry, Current) = (entry, entry);

            /// <summary>
            /// Moves forward to the next inner phase. If this was the last one
            /// this method will return false.
            /// <br />
            /// If the last inner phase was executed this will reset its state.
            /// </summary>
            /// <returns>true if a next inner phase exists.</returns>
            public bool GoNext(bool checkGroup)
            {
                if (checkGroup && Current.Phase.TryPickT1(out PhaseGroup phaseGroup, out _))
                {
                    if (phaseGroup.GoNext(true))
                        return true;
                }
                if (Current.Next != null)
                {
                    Current = Current.Next;
                    return true;
                }
                else
                {
                    Current = Entry;
                    return false;
                }
            }

            /// <summary>
            /// Initialize the current step if not entered before otherwise it will
            /// move forward to the next step. If the current step contains a
            /// <see cref="PhaseGroup"/> it will try to initialize (or move) this
            /// one first.
            /// <br />
            /// If a inner group is at the end and executed during the current runtime
            /// it will execute this group again.
            /// <br />
            /// This method returns false if none of the inner phases (including
            /// the inner groups) can be executed next.
            /// </summary>
            /// <returns></returns>
            public bool InitGoNext()
            {
                if (!IsEntered)
                {
                    IsEntered = true;
                    IsExecuted = false;
                    if (Current.Phase.IsT0)
                        return true;
                }
                bool hasPhaseSkipped = false;
                bool checkGroup;
                do
                {
                    checkGroup = true;
                    if (Current.Phase.TryPickT1(out PhaseGroup phaseGroup, out _))
                    {
                        hasPhaseSkipped = true;
                        if (phaseGroup.InitGoNext())
                            return true;
                        else if (phaseGroup.IsExecuted)
                        {
                            phaseGroup.IsExecuted = false;
                            if (phaseGroup.InitGoNext())
                                return true;
                        }
                        else checkGroup = false;
                    }
                    else
                    {
                        if (hasPhaseSkipped)
                            return true;
                        else hasPhaseSkipped = true;
                    }
                }
                while (GoNext(checkGroup));
                IsEntered = false;
                return false;
            }
        }

        public Step InitialStep { get; }

        public Step CurrentStep { get; private set; }

        public Phase Current => CurrentStep.CurrentPhase;

        public Stage Stage => CurrentStep.CurrentStep.Stage;

        internal PhaseFlow(Step step)
            => InitialStep = CurrentStep = new Step(new InitialStage(), new InitialPhase())
            {
                Next = step
            };

        private bool Next()
        {
            foreach (var voting in Current.Votings.ToArray())
                Current.RemoveVoting(voting);
            if (CurrentStep.Phase.TryPickT1(out PhaseGroup phaseGroup, out _))
            {
                if (phaseGroup.InitGoNext())
                    return true;
                else if (phaseGroup.IsExecuted)
                {
                    phaseGroup.IsExecuted = false;
                    if (phaseGroup.InitGoNext())
                        return true;
                }
            }
            var next = CurrentStep.Next;
            if (next == null)
                return false;
            if (next.Phase.TryPickT1(out phaseGroup, out _) && !phaseGroup.InitGoNext())
                return false;
            CurrentStep = next;
            return true;
        }

        public async Task<bool> NextAsync(GameRoom game)
        {
            var lastStage = Stage;
            while (Next())
            {
                if (!Current.CanExecute(game))
                    continue;

                if (Current.IsGamePhase)
                {
                    if (Stage != lastStage)
                    {
                        game.SendEvent(new Events.SendStage(Stage));
                        lastStage = Stage;
                    }
                    game.SendEvent(new Events.NextPhase(Current));
                    CurrentStep.SetExecute();
                }

                await Current.InitAsync(game);

                if (game.Phase == null)
                    return false;

                if (!Current.IsGamePhase)
                    continue;

                if (game.AutoFinishRounds && !Current.Votings.Any())
                    continue;

                return true;
            }
            return false;
        }
    }
}