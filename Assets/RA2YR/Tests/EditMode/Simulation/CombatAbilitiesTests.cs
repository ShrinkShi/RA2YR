using System.Linq;
using NUnit.Framework;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode.Simulation
{
    public sealed class CombatAbilitiesTests
    {
        private static EntityId E(int index) => new EntityId(index, 1);
        private static WeaponRuntimeDescriptor Weapon(int damage = 5, int range = 3, long cooldown = 2) => new WeaponRuntimeDescriptor(7, range, cooldown, damage);

        [Test] public void AttackRequiresRangeAndTargetValidity()
        { var p = new AttackProposal(E(0), E(1), 7, 4, 4, 1); Assert.That(CombatResolver.ValidateAttack(p, Weapon(), true, new WeaponCooldownState(0)).Status, Is.EqualTo(AttackValidationStatus.OutOfRange)); Assert.That(CombatResolver.ValidateAttack(p, Weapon(), false, new WeaponCooldownState(0)).Status, Is.EqualTo(AttackValidationStatus.InvalidTarget)); }
        [Test] public void AttackHonorsCooldownAndEmitsDamageOnlyAfterValidation()
        { var p = new AttackProposal(E(0), E(1), 7, 1, 1, 2); var r = CombatResolver.ValidateAttack(p, Weapon(), true, new WeaponCooldownState(2)); Assert.That(r.IsAccepted, Is.False); Assert.That(r.Damage.Amount, Is.EqualTo(0)); var ok = CombatResolver.ValidateAttack(new AttackProposal(E(0), E(1), 7, 2, 1, 2), Weapon(), true, new WeaponCooldownState(2)); Assert.That(ok.IsAccepted, Is.True); Assert.That(ok.Damage.Amount, Is.EqualTo(5)); }
        [Test] public void DamageCommitCanonicalOrderAndDeathAreDeterministic()
        { var ledger = new CombatHealthLedger(); ledger.Register(new Health(E(1), 10, 10)); var result = ledger.Commit(new[] { new DamageEvent(E(2), E(1), 4, 1, 3, 2), new DamageEvent(E(0), E(1), 6, 1, 3, 1) }); Assert.That(result.Health.Single(x => x.Entity == E(1)).Current, Is.EqualTo(0)); Assert.That(result.Deaths.Single().Entity, Is.EqualTo(E(1))); }
        [Test] public void WorkersCannotMutateHealthBeforeCommit()
        { var ledger = new CombatHealthLedger(); ledger.Register(new Health(E(1), 10, 10)); var proposal = CombatResolver.ValidateAttack(new AttackProposal(E(0), E(1), 7, 0, 1, 0), Weapon(), true, new WeaponCooldownState(0)); Health current; Assert.That(ledger.TryGet(E(1), out current), Is.True); Assert.That(current.Current, Is.EqualTo(10)); Assert.That(ledger.Commit(new[] { proposal.Damage }).Health.Single(x => x.Entity == E(1)).Current, Is.EqualTo(5)); }
        [Test] public void MissingWeaponFailsClosed()
        { var r = CombatResolver.ValidateAttack(new AttackProposal(E(0), E(1), 7, 0, 1, 0), null, true, new WeaponCooldownState(0)); Assert.That(r.Status, Is.EqualTo(AttackValidationStatus.InvalidWeapon)); }
        [Test] public void AbilityManualAndCapabilityGatesAreExplicit()
        { var d = new AbilityDescriptor(1, 3, 2, 4, 2); var s = new AbilityState(0, 5); var c = new[] { new AbilityTargetCandidate(E(2), 1, 8, 0, 2, false, 0) }; AbilityUseProposal p; CombatDiagnostic diag; Assert.That(AbilityEvaluator.TryPropose(E(0), d, s, new AbilityDecisionProfile(true, 1, 4), AutonomyResolver.Resolve(AutonomyCapabilities.AutoCast, AutonomyOverride.Manual, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified), c, 0, 1, out p, out diag), Is.False); Assert.That(AbilityEvaluator.TryPropose(E(0), d, s, new AbilityDecisionProfile(true, 1, 4), AutonomyResolver.Resolve(AutonomyCapabilities.AutoCast, AutonomyOverride.Automatic, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified), c, 0, 1, out p, out diag), Is.True); }
        [Test] public void AbilityRejectsFriendlyFireAndHonorsBudget()
        { var d = new AbilityDescriptor(1, 3, 0, 4, 1); var s = new AbilityState(0, 0); var c = new[] { new AbilityTargetCandidate(E(2), 1, 100, 0, 0, true, 0), new AbilityTargetCandidate(E(3), 2, 100, 0, 0, false, 0) }; AbilityUseProposal p; CombatDiagnostic diag; Assert.That(AbilityEvaluator.TryPropose(E(0), d, s, new AbilityDecisionProfile(true, 0, 1), AutonomyResolver.Resolve(AutonomyCapabilities.AutoCast, AutonomyOverride.Automatic, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified), c, 0, 1, out p, out diag), Is.False); }
        [Test] public void AbilityCandidateOrderingIsStable()
        { var d = new AbilityDescriptor(1, 0, 0, 4, 2); var s = new AbilityState(0, 0); var c = new[] { new AbilityTargetCandidate(E(3), 1, 8, 0, 1, false, 0), new AbilityTargetCandidate(E(2), 1, 8, 0, 1, false, 0) }; AbilityUseProposal p; CombatDiagnostic diag; Assert.That(AbilityEvaluator.TryPropose(E(0), d, s, new AbilityDecisionProfile(true, 0, 4), AutonomyResolver.Resolve(AutonomyCapabilities.AutoCast, AutonomyOverride.Automatic, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified), c, 0, 1, out p, out diag), Is.True); Assert.That(p.Target, Is.EqualTo(E(2))); }
        [Test] public void RetreatAndCrushProposalsRespectAutonomyEnvelope()
        { var auto = AutonomyResolver.Resolve(AutonomyCapabilities.Evade, AutonomyOverride.Automatic, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified); var manual = AutonomyResolver.Resolve(AutonomyCapabilities.Evade, AutonomyOverride.Manual, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified); var crush = new CrushThreatCandidate(E(1), 5, new CellCoordinate(2, 2), true); Assert.That(crush.ShouldEvade(auto), Is.True); Assert.That(crush.ShouldEvade(manual), Is.False); var retreat = new RetreatProposal(E(1), new CellCoordinate(3, 3), 4, 5, 2, 1, 0); Assert.That(retreat.Utility, Is.EqualTo(11)); }
        [Test] public void CooldownArithmeticIsCheckedAndImmutable()
        { var state = new WeaponCooldownState(1); Assert.That(state.After(2, 3).ReadyTick, Is.EqualTo(5)); Assert.That(state.ReadyTick, Is.EqualTo(1)); }
        [Test] public void DamageBudgetFailsClosed()
        { var ledger = new CombatHealthLedger(maxEvents: 1); ledger.Register(new Health(E(1), 5, 5)); var result = ledger.Commit(new[] { new DamageEvent(E(0), E(1), 1, 1, 0, 0), new DamageEvent(E(0), E(1), 1, 1, 0, 1) }); Assert.That(result.IsSuccess, Is.False); Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(CombatDiagnosticCode.BudgetExceeded)); }
    }
}
