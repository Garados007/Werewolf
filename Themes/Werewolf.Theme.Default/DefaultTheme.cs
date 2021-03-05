using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Werewolf.Theme.Default
{
    public class DefaultTheme : Theme
    {
        public DefaultTheme(GameRoom? game, UserFactory users) : base(game, users)
        {
        }

        public override Role GetBasicRole()
            => new Roles.Unknown(this);

        public override IEnumerable<Role> GetRoleTemplates()
        {
            yield return new Roles.Villager(this);
            yield return new Roles.Hunter(this);
            yield return new Roles.Werwolf(this);
            yield return new Roles.Oracle(this);
            yield return new Roles.Girl(this);
            yield return new Roles.Amor(this);
            yield return new Roles.Witch(this);
            yield return new Roles.Healer(this);
            yield return new Roles.Idiot(this);
            yield return new Roles.OldMan(this);
            yield return new Roles.ScapeGoat(this);
            yield return new Roles.Flutist(this);
            yield return new Roles.PureSoul(this);
            yield return new Roles.TwoSisters(this);
            yield return new Roles.ThreeBrothers(this);
            yield return new Roles.Angel(this);
        }

        public override PhaseFlow GetPhases(IDictionary<Role, int> roles)
        {
            // init stages
            var nightStage = new Stages.NightStage();
            var morningStage = new Stages.MorningStage();
            var dayStage = new Stages.DayStage();
            var afternoonStage = new Stages.AfternoonStage();

            static PhaseFlow.PhaseGroup KillHandling(Stage stage)
            {
                var phases = new PhaseFlowBuilder();
                phases.Add(stage);
                // remove flags if possible
                phases.Add(new Phases.KillFlagWerwolfVictimAction());
                // transition and execute special actions
                phases.Add(new Werewolf.Theme.Phases.KillTransitionToAboutToKillAction());
                // transition to prepare special phases
                phases.Add(new Werewolf.Theme.Phases.KillTransitionToBeforeKillAction());
                // special phases
                phases.Add(new Phases.HunterPhase());
                phases.Add(new Phases.ScapeGoatPhase());
                phases.Add(new Phases.InheritMajorPhase());
                // lastly kill and check for game end
                phases.Add(new Werewolf.Theme.Phases.KillTransitionToKilledAction());
                phases.Add(new Werewolf.Theme.Phases.CheckWinConditionAction());
                return phases.BuildGroup() ?? throw new InvalidOperationException();
            }

            static PhaseFlow.PhaseGroup DailyLoop(Stage night, Stage morning, Stage day, Stage afternoon, IDictionary<Role, int> roles)
            {
                var phases = new PhaseFlowBuilder();

                void AddNight()
                {

                    // add night phases
                    phases.Add(night);
                    phases.Add(new Phase[]
                    {
                        new Phases.HealerPhase(),
                        new Phases.OraclePhase(),
                        new Phases.WerwolfPhase(),
                        new Phases.WitchPhase(),
                        new Phases.FlutistPhase(),
                        new Phases.TwoSisterDiscussionPhase(),
                        new Phases.ThreeBrotherDiscussionPhase(),
                    });

                    // add morning phases
                    phases.Add(morning);
                    phases.Add(KillHandling(morning));
                    phases.Add(new Phases.AngelMiss());
                }
                void AddDay()
                {

                    // add day phases
                    phases.Add(day);
                    phases.Add(new Phase[]
                    {
                        new Phases.ElectMajorPhase(),
                        new Phases.DailyVictimElectionPhase(),
                    });

                    // add afternoon phases
                    phases.Add(afternoon);
                    phases.Add(KillHandling(afternoon));
                }

                bool HasAngel()
                {
                    foreach (var (role, count) in roles)
                        if (role is Roles.Angel)
                            return count > 0;
                    return false;
                }

                if (HasAngel())
                {
                    AddDay();
                    AddNight();
                }
                else
                {
                    AddNight();
                    AddDay();
                }

                // return
                return phases.BuildGroup() ?? throw new InvalidOperationException();
            }

            // build phases
            var phases = new PhaseFlowBuilder();
            phases.Add(nightStage);
            phases.Add(new Phases.AmorPhase());

            phases.Add(DailyLoop(nightStage, morningStage, dayStage, afternoonStage, roles));

            return phases.BuildPhaseFlow() ?? throw new InvalidOperationException();
        }

        public override IEnumerable<WinConditionCheck> GetWinConditions()
        {
            yield return AngelDied;
            yield return OnlyLovedOnes;
            yield return OnlyEnchanted;
        }

        public static bool AngelDied(GameRoom game, [NotNullWhen(true)] out ReadOnlyMemory<Role>? winner)
        {
            winner = game.Participants.Values
                .Where(x => x is Roles.Angel angel && angel.KillState == KillState.Killed && !angel.MissedFirstRound)
                .Cast<Role>()
                .ToArray();
            return winner.Value.Length > 0;
        }

        private static bool OnlyLovedOnes(GameRoom game, [NotNullWhen(true)] out ReadOnlyMemory<Role>? winner)
        {
            foreach (var player in game.NotKilledRoles)
                if (player is BaseRole baseRole && !baseRole.IsLoved)
                {
                    winner = null;
                    return false;
                }
            winner = game.AliveRoles.ToArray();
            return true;
        }

        private static bool OnlyEnchanted(GameRoom game, [NotNullWhen(true)] out ReadOnlyMemory<Role>? winner)
        {
            foreach (var player in game.NotKilledRoles)
                if (player is BaseRole baseRole && !(baseRole.IsEnchantedByFlutist || player is Roles.Flutist))
                {
                    winner = null;
                    return false;
                }
            winner = game.Participants.Values
                .Where(x => x is Roles.Flutist).Cast<Role>().ToArray();
            return true;
        }

        public override bool CheckRoleUsage(Role role, ref int count, int oldCount, [NotNullWhen(false)] out string? error)
        {
            if (role is Roles.TwoSisters)
            {
                if (count < 0)
                    count = 0;
                else if (count > 2)
                    count = 2;
                else if (count is > 0 and < 2)
                {
                    count = oldCount < count ? 2 : 0;
                }
                error = null;
                return true;
            }
            if (role is Roles.ThreeBrothers)
            {
                if (count < 0)
                    count = 0;
                else if (count > 3)
                    count = 3;
                else if (count is > 0 and < 3)
                {
                    count = oldCount < count ? 3 : 0;
                }
                error = null;
                return true;
            }
            return base.CheckRoleUsage(role, ref count, oldCount, out error);
        }
    }
}
