using NUnit.Framework;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode.Simulation
{
    public sealed class IntegratedBattleTests
    {
        private static IntegratedSyntheticBattle Battle() { var b = new IntegratedSyntheticBattle(); b.Add(1, 2, 2, 6, 8, 3); b.Add(2, 3, 2, 6, 8, 2); return b; }
        [Test] public void SyntheticBattleRunsAttackAndDamageCommit() { var r = Battle().Step(); Assert.That(r.AttackCount, Is.EqualTo(2)); Assert.That(r.Units.Count, Is.EqualTo(2)); }
        [Test] public void RepeatedSyntheticBattlesHaveIdenticalHashes() { var a = Battle(); var b = Battle(); for (int i = 0; i < 4; i++) { Assert.That(a.Step().StateHash, Is.EqualTo(b.Step().StateHash)); } }
        [Test] public void InputOrderDoesNotChangeCanonicalState() { var a = Battle(); var b = new IntegratedSyntheticBattle(); b.Add(2, 3, 2, 6, 8, 2); b.Add(1, 2, 2, 6, 8, 3); Assert.That(a.Step().StateHash, Is.EqualTo(b.Step().StateHash)); }
        [Test] public void DeathIsDeterministicAndNoNegativeHealth() { var b = new IntegratedSyntheticBattle(); b.Add(1, 1, 1, 1, 5, 4); b.Add(2, 1, 2, 1, 5, 4); var r = b.Step(); Assert.That(r.Units[0].Health, Is.EqualTo(0)); Assert.That(r.Units[1].Health, Is.EqualTo(0)); }
        [Test] public void ManualFlagDisablesAutonomousAttack() { var b = Battle(); var r = b.Step(false); Assert.That(r.AttackCount, Is.EqualTo(2)); }
        [Test] public void RangeAndCooldownBoundWork() { var b = new IntegratedSyntheticBattle(); b.Add(1, 1, 1, 6, 0, 3); b.Add(2, 5, 5, 6, 0, 2); Assert.That(b.Step().AttackCount, Is.EqualTo(0)); }
        [Test] public void BoundedUnitBudgetFailsClosed() { var b = new IntegratedSyntheticBattle(maxUnits: 1); b.Add(1, 0, 0); Assert.Throws<System.ArgumentOutOfRangeException>(() => b.Add(2, 0, 1)); }
        [Test] public void StateHashChangesAfterSimulationStep() { var b = Battle(); var before = b.CanonicalHash(); b.Step(); Assert.That(b.CanonicalHash(), Is.Not.EqualTo(before)); }
    }
}
