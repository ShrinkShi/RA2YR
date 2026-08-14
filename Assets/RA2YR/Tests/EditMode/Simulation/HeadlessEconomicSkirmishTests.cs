using NUnit.Framework;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode.Simulation
{
    public sealed class HeadlessEconomicSkirmishTests
    {
        private static HeadlessSkirmishConfig Config(EconomicAgentStrategyProfile first = EconomicAgentStrategyProfile.Rush, EconomicAgentStrategyProfile second = EconomicAgentStrategyProfile.Turtle, bool agents = true)
        {
            return new HeadlessSkirmishConfig(11, 32, 40, 10, 10, 20, 30, agents, first, second);
        }

        [Test] public void StartsWithTwoAlivePlayersAndExplicitStrategies() { var r = new HeadlessEconomicSkirmish(Config()).Step(); Assert.That(r.Players.Count, Is.EqualTo(2)); Assert.That(r.Players[0].State, Is.EqualTo(HeadlessPlayerState.Alive)); Assert.That(r.Players[0].Strategy, Is.EqualTo(EconomicAgentStrategyProfile.Rush)); }
        [Test] public void HarvestProducesIncomeAndResourceEvidence() { var r = new HeadlessEconomicSkirmish(Config()).Step(); Assert.That(r.HarvestEvents, Is.EqualTo(2)); Assert.That(r.IncomeEvents, Is.EqualTo(2)); Assert.That(r.Players[0].CollectedResource, Is.EqualTo(40)); Assert.That(r.Players[0].Credits, Is.EqualTo(0)); }
        [Test] public void ConstructPowerAndFactoryUseEconomyAuthority() { var s = new HeadlessEconomicSkirmish(Config()); var r = s.Step(); Assert.That(r.PowerBuilds, Is.EqualTo(2)); Assert.That(r.FactoryBuilds, Is.EqualTo(2)); Assert.That(r.Players[0].HasPower, Is.True); Assert.That(r.Players[0].HasFactory, Is.True); }
        [Test] public void ProductionRallyAndAttackEnterCommandStream() { var r = new HeadlessEconomicSkirmish(Config()).Step(); Assert.That(r.ProductionEvents, Is.EqualTo(2)); Assert.That(r.RallyEvents, Is.EqualTo(2)); Assert.That(r.Commands, Has.Count.EqualTo(2)); Assert.That(r.Commands[0].Source, Is.EqualTo(CommandSource.ComputerAI)); }
        [Test] public void AiVsAiReachesTerminalWinnerAndStructureDestruction() { var s = new HeadlessEconomicSkirmish(Config()); var r = s.RunToCompletion(); Assert.That(r.IsSuccess, Is.True); Assert.That(r.MatchComplete, Is.True); Assert.That(r.Winner.HasValue, Is.True); Assert.That(r.DestroyedStructures, Is.EqualTo(1)); Assert.That(r.Players, Has.Some.Matches<HeadlessPlayerSnapshot>(x => x.State == HeadlessPlayerState.Defeated)); }
        [Test] public void RepeatedRunsHaveIdenticalFinalHash() { var a = new HeadlessEconomicSkirmish(Config()); var b = new HeadlessEconomicSkirmish(Config()); Assert.That(a.RunToCompletion().StateHash, Is.EqualTo(b.RunToCompletion().StateHash)); }
        [Test] public void StrategyConfigurationChangesWinnerDamagePolicy() { var rush = new HeadlessEconomicSkirmish(Config(EconomicAgentStrategyProfile.Rush, EconomicAgentStrategyProfile.Turtle)).RunToCompletion(); var turtle = new HeadlessEconomicSkirmish(Config(EconomicAgentStrategyProfile.Turtle, EconomicAgentStrategyProfile.Rush)).RunToCompletion(); Assert.That(rush.Winner, Is.EqualTo(new PlayerId(0))); Assert.That(turtle.Winner, Is.EqualTo(new PlayerId(1))); }
        [Test] public void ManualCommandStreamWorksWithoutComputerAgents() { var s = new HeadlessEconomicSkirmish(Config(agents: false)); s.QueueCommand(new CommandRequest(0, new EntityId(100, 1), CommandSource.Human, CommandKind.Attack, new CommandTarget(new CellCoordinate(1, 0), null, "enemy-base"), QueueMode.Replace, 1)); var r = s.Step(); Assert.That(r.ManualCommands, Is.EqualTo(1)); Assert.That(r.Commands[0].Source, Is.EqualTo(CommandSource.Human)); Assert.That(r.AttackEvents, Is.EqualTo(1)); }
        [Test] public void CommandStreamUsesExistingCommandRequestContract() { var s = new HeadlessEconomicSkirmish(Config()); s.QueueCommand(new CommandRequest(9, new EntityId(100, 1), CommandSource.Script, CommandKind.Guard, new CommandTarget(new CellCoordinate(0, 0), null), QueueMode.Append, 1)); var r = s.Step(); Assert.That(r.Commands, Has.Some.Matches<CommandRequest>(x => x.CommandId == 9 && x.Source == CommandSource.Script)); }
        [Test] public void StateHashIncludesCommandsAndAuthoritativeEconomy() { var a = new HeadlessEconomicSkirmish(Config()); var b = new HeadlessEconomicSkirmish(Config()); a.QueueCommand(new CommandRequest(9, new EntityId(100, 1), CommandSource.Script, CommandKind.Guard, new CommandTarget(new CellCoordinate(0, 0), null), QueueMode.Append, 1)); Assert.That(a.Step().StateHash, Is.Not.EqualTo(b.Step().StateHash)); }
        [Test] public void TickBudgetFailsClosedBeforeTerminalState() { var r = new HeadlessEconomicSkirmish(new HeadlessSkirmishConfig(1, 1, 0, 0, 0, 100, 100, true, EconomicAgentStrategyProfile.Turtle, EconomicAgentStrategyProfile.Turtle)).RunToCompletion(); Assert.That(r.CompletionStatus, Is.EqualTo(HeadlessSkirmishCompletionStatus.Failed)); Assert.That(r.Diagnostics, Has.Some.Matches<HeadlessSkirmishDiagnostic>(x => x.Code == HeadlessSkirmishDiagnosticCode.NoProgress)); }
        [Test] public void CoreSkirmishAssemblyHasNoUnityReferences() { Assert.That(typeof(HeadlessEconomicSkirmish).Assembly.GetReferencedAssemblies(), Has.None.Matches<System.Reflection.AssemblyName>(x => x.Name == "UnityEngine" || x.Name == "UnityEditor")); }
    }
}
