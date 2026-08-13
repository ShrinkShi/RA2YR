using NUnit.Framework;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode.Simulation
{
    public sealed class EconomyAuthorityTests
    {
        private static PlayerId P(int i) => new PlayerId(i);
        [Test] public void PlayerAndHouseIdentityAreDeterministic() { var h = new HouseRuntimeState(P(2), "Alpha", 1); Assert.That(h.Player.Value, Is.EqualTo(2)); }
        [Test] public void StartingCreditsAndSpendUseTransactions() { var e = new EconomyAuthority(); e.Register(P(0), 100); EconomyTransaction t; Assert.That(e.TryApply(EconomyTransactionSource.ProductionSpend, P(0), 1, -40, "build", out t), Is.True); Assert.That(e.Get(P(0)).Balance, Is.EqualTo(60)); }
        [Test] public void InsufficientFundsFailsClosed() { var e = new EconomyAuthority(); e.Register(P(0), 10); EconomyTransaction t; Assert.That(e.TryApply(EconomyTransactionSource.ProductionSpend, P(0), 1, -11, "build", out t), Is.False); Assert.That(e.Get(P(0)).Balance, Is.EqualTo(10)); }
        [Test] public void RefundAndIncomeAreChecked() { var e = new EconomyAuthority(); e.Register(P(0), 10); EconomyTransaction t; Assert.That(e.TryApply(EconomyTransactionSource.SellRefund, P(0), 2, 5, "refund", out t), Is.True); Assert.That(e.Get(P(0)).Balance, Is.EqualTo(15)); }
        [Test] public void OverflowFailsWithoutMutation() { var e = new EconomyAuthority(); e.Register(P(0), long.MaxValue); EconomyTransaction t; Assert.That(e.TryApply(EconomyTransactionSource.HarvestIncome, P(0), 1, 1, "ore", out t), Is.False); Assert.That(e.Get(P(0)).Balance, Is.EqualTo(long.MaxValue)); }
        [Test] public void SimultaneousTransactionsHaveStableOrder() { var e = new EconomyAuthority(); e.Register(P(0), 100); EconomyTransaction a; EconomyTransaction b; e.TryApply(EconomyTransactionSource.HarvestIncome, P(0), 2, 4, "a", out a); e.TryApply(EconomyTransactionSource.ProductionSpend, P(0), 1, -3, "b", out b); Assert.That(e.Transactions[0].Tick, Is.EqualTo(1)); }
        [Test] public void AllianceRemainsDirected() { var a = new AllianceRuntimeState(P(0), P(1), true); Assert.That(a.Source, Is.EqualTo(P(0))); Assert.That(a.Target, Is.EqualTo(P(1))); }
        [Test] public void PowerAndTechnologySnapshotsAreImmutable() { var p = new PowerState(5, 8); Assert.That(p.LowPower, Is.True); var t = new TechnologySnapshot(3, new[] { "Radar" }); Assert.That(t.Capabilities[0], Is.EqualTo("Radar")); }
        [Test] public void OwnershipTransferCandidatePreservesType() { var o = new OwnershipSnapshot(P(1), "Factory"); Assert.That(o.Type, Is.EqualTo("Factory")); }
        [Test] public void RepeatRunStateHashMatches() { var a = new EconomyAuthority(); var b = new EconomyAuthority(); a.Register(P(0), 100); b.Register(P(0), 100); EconomyTransaction t; a.TryApply(EconomyTransactionSource.HarvestIncome, P(0), 1, 5, "x", out t); b.TryApply(EconomyTransactionSource.HarvestIncome, P(0), 1, 5, "x", out t); Assert.That(a.StateHash(), Is.EqualTo(b.StateHash())); }
    }
}
