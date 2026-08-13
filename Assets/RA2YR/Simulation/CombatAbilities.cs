using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RA2YR.Simulation
{
    public enum CombatDiagnosticCode
    {
        InvalidWeapon,
        InvalidTarget,
        OutOfRange,
        Cooldown,
        InvalidDamage,
        DuplicateEvent,
        HealthOverflow,
        BudgetExceeded,
        AbilityUnavailable,
        AbilityCooldown,
        InvalidAbilityTarget,
        FriendlyFireCandidate,
        RetreatUnavailable,
        CrushThreatUnavailable
    }

    public sealed class CombatDiagnostic
    {
        public CombatDiagnostic(CombatDiagnosticCode code, EntityId source, EntityId target, long tick, string message)
        { Code = code; Source = source; Target = target; Tick = tick; Message = message ?? string.Empty; }
        public CombatDiagnosticCode Code { get; }
        public EntityId Source { get; }
        public EntityId Target { get; }
        public long Tick { get; }
        public string Message { get; }
    }

    public readonly struct WeaponRuntimeDescriptor
    {
        public WeaponRuntimeDescriptor(int weaponId, int range, long cooldownTicks, int damage)
        {
            if (weaponId < 0 || range < 0 || cooldownTicks < 0 || damage < 0) throw new ArgumentOutOfRangeException();
            WeaponId = weaponId; Range = range; CooldownTicks = cooldownTicks; Damage = damage;
        }
        public int WeaponId { get; }
        public int Range { get; }
        public long CooldownTicks { get; }
        public int Damage { get; }
    }

    public readonly struct WeaponCooldownState
    {
        public WeaponCooldownState(long readyTick) { if (readyTick < 0) throw new ArgumentOutOfRangeException(nameof(readyTick)); ReadyTick = readyTick; }
        public long ReadyTick { get; }
        public bool IsReady(long tick) => tick >= ReadyTick;
        public WeaponCooldownState After(long tick, long duration) => new WeaponCooldownState(checked(tick + duration));
    }

    public readonly struct AttackProposal : IComparable<AttackProposal>
    {
        public AttackProposal(EntityId source, EntityId target, int weaponId, long tick, int distance, long ordinal)
        { Source = source; Target = target; WeaponId = weaponId; Tick = tick; Distance = distance; Ordinal = ordinal; }
        public EntityId Source { get; }
        public EntityId Target { get; }
        public int WeaponId { get; }
        public long Tick { get; }
        public int Distance { get; }
        public long Ordinal { get; }
        public int CompareTo(AttackProposal other)
        { int c = Tick.CompareTo(other.Tick); if (c != 0) return c; c = Source.CompareTo(other.Source); if (c != 0) return c; c = Target.CompareTo(other.Target); return c != 0 ? c : Ordinal.CompareTo(other.Ordinal); }
    }

    public enum AttackValidationStatus { Accepted, InvalidWeapon, InvalidTarget, OutOfRange, Cooldown }
    public sealed class AttackValidationResult
    {
        internal AttackValidationResult(AttackValidationStatus status, AttackProposal proposal, DamageEvent damage, IEnumerable<CombatDiagnostic> diagnostics)
        { Status = status; Proposal = proposal; Damage = damage; Diagnostics = new ReadOnlyCollection<CombatDiagnostic>((diagnostics ?? Enumerable.Empty<CombatDiagnostic>()).ToList()); }
        public AttackValidationStatus Status { get; }
        public bool IsAccepted => Status == AttackValidationStatus.Accepted;
        public AttackProposal Proposal { get; }
        public DamageEvent Damage { get; }
        public IReadOnlyList<CombatDiagnostic> Diagnostics { get; }
    }

    public readonly struct DamageEvent : IComparable<DamageEvent>
    {
        public DamageEvent(EntityId source, EntityId target, int amount, int weaponId, long tick, long ordinal)
        { if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); Source = source; Target = target; Amount = amount; WeaponId = weaponId; Tick = tick; Ordinal = ordinal; }
        public EntityId Source { get; }
        public EntityId Target { get; }
        public int Amount { get; }
        public int WeaponId { get; }
        public long Tick { get; }
        public long Ordinal { get; }
        public int CompareTo(DamageEvent other)
        { int c = Tick.CompareTo(other.Tick); if (c != 0) return c; c = Target.CompareTo(other.Target); if (c != 0) return c; c = Source.CompareTo(other.Source); return c != 0 ? c : Ordinal.CompareTo(other.Ordinal); }
    }

    public readonly struct Health
    {
        public Health(EntityId entity, int current, int maximum)
        { if (!entity.IsValid || maximum < 0 || current < 0 || current > maximum) throw new ArgumentOutOfRangeException(); Entity = entity; Current = current; Maximum = maximum; }
        public EntityId Entity { get; }
        public int Current { get; }
        public int Maximum { get; }
        public bool IsDead => Current == 0;
        public Health Apply(int damage) => new Health(Entity, damage >= Current ? 0 : Current - damage, Maximum);
    }

    public readonly struct Death : IComparable<Death>
    {
        public Death(EntityId entity, EntityId source, long tick, long ordinal) { Entity = entity; Source = source; Tick = tick; Ordinal = ordinal; }
        public EntityId Entity { get; }
        public EntityId Source { get; }
        public long Tick { get; }
        public long Ordinal { get; }
        public int CompareTo(Death other) { int c = Tick.CompareTo(other.Tick); return c != 0 ? c : Entity.CompareTo(other.Entity); }
    }

    public sealed class DamageCommitResult
    {
        internal DamageCommitResult(IEnumerable<Health> health, IEnumerable<Death> deaths, IEnumerable<CombatDiagnostic> diagnostics)
        { Health = new ReadOnlyCollection<Health>((health ?? Enumerable.Empty<Health>()).ToList()); Deaths = new ReadOnlyCollection<Death>((deaths ?? Enumerable.Empty<Death>()).ToList()); Diagnostics = new ReadOnlyCollection<CombatDiagnostic>((diagnostics ?? Enumerable.Empty<CombatDiagnostic>()).ToList()); }
        public IReadOnlyList<Health> Health { get; }
        public IReadOnlyList<Death> Deaths { get; }
        public IReadOnlyList<CombatDiagnostic> Diagnostics { get; }
        public bool IsSuccess => Diagnostics.Count == 0;
    }

    public sealed class CombatHealthLedger
    {
        private readonly Dictionary<EntityId, Health> health = new Dictionary<EntityId, Health>();
        private readonly int maxEvents;
        public CombatHealthLedger(int maxEntities = 256, int maxEvents = 1024) { if (maxEntities <= 0 || maxEvents <= 0) throw new ArgumentOutOfRangeException(); this.maxEvents = maxEvents; Capacity = maxEntities; }
        public int Capacity { get; }
        public void Register(Health value) { if (health.Count >= Capacity && !health.ContainsKey(value.Entity)) throw new InvalidOperationException("Health budget exceeded."); health[value.Entity] = value; }
        public bool TryGet(EntityId entity, out Health value) => health.TryGetValue(entity, out value);
        public DamageCommitResult Commit(IEnumerable<DamageEvent> input)
        {
            var events = (input ?? Enumerable.Empty<DamageEvent>()).ToList();
            if (events.Count > maxEvents) return new DamageCommitResult(Array.Empty<Health>(), Array.Empty<Death>(), new[] { new CombatDiagnostic(CombatDiagnosticCode.BudgetExceeded, default(EntityId), default(EntityId), 0, "Damage event budget exceeded.") });
            events.Sort();
            var deaths = new List<Death>(); var diagnostics = new List<CombatDiagnostic>();
            foreach (DamageEvent e in events)
            {
                Health current; if (!health.TryGetValue(e.Target, out current)) { diagnostics.Add(new CombatDiagnostic(CombatDiagnosticCode.InvalidTarget, e.Source, e.Target, e.Tick, "Target health is not registered.")); continue; }
                Health next = current.Apply(e.Amount); health[e.Target] = next;
                if (!current.IsDead && next.IsDead) deaths.Add(new Death(e.Target, e.Source, e.Tick, e.Ordinal));
            }
            return new DamageCommitResult(health.Values.OrderBy(x => x.Entity), deaths.OrderBy(x => x), diagnostics);
        }
    }

    public static class CombatResolver
    {
        public static AttackValidationResult ValidateAttack(AttackProposal proposal, WeaponRuntimeDescriptor? descriptor, bool targetValid, WeaponCooldownState cooldown)
        {
            var diagnostics = new List<CombatDiagnostic>();
            if (!descriptor.HasValue) { diagnostics.Add(new CombatDiagnostic(CombatDiagnosticCode.InvalidWeapon, proposal.Source, proposal.Target, proposal.Tick, "Weapon descriptor is unavailable.")); return new AttackValidationResult(AttackValidationStatus.InvalidWeapon, proposal, default(DamageEvent), diagnostics); }
            WeaponRuntimeDescriptor weapon = descriptor.Value;
            if (!targetValid || !proposal.Target.IsValid) { diagnostics.Add(new CombatDiagnostic(CombatDiagnosticCode.InvalidTarget, proposal.Source, proposal.Target, proposal.Tick, "Target is not valid.")); return new AttackValidationResult(AttackValidationStatus.InvalidTarget, proposal, default(DamageEvent), diagnostics); }
            if (proposal.Distance < 0 || proposal.Distance > weapon.Range) { diagnostics.Add(new CombatDiagnostic(CombatDiagnosticCode.OutOfRange, proposal.Source, proposal.Target, proposal.Tick, "Target is outside weapon range.")); return new AttackValidationResult(AttackValidationStatus.OutOfRange, proposal, default(DamageEvent), diagnostics); }
            if (!cooldown.IsReady(proposal.Tick)) { diagnostics.Add(new CombatDiagnostic(CombatDiagnosticCode.Cooldown, proposal.Source, proposal.Target, proposal.Tick, "Weapon cooldown is active.")); return new AttackValidationResult(AttackValidationStatus.Cooldown, proposal, default(DamageEvent), diagnostics); }
            return new AttackValidationResult(AttackValidationStatus.Accepted, proposal, new DamageEvent(proposal.Source, proposal.Target, weapon.Damage, weapon.WeaponId, proposal.Tick, proposal.Ordinal), diagnostics);
        }
    }

    public readonly struct AbilityDescriptor
    {
        public AbilityDescriptor(int abilityId, long cooldownTicks, int resourceCost, int range, int maxTargets)
        { if (abilityId < 0 || cooldownTicks < 0 || resourceCost < 0 || range < 0 || maxTargets <= 0) throw new ArgumentOutOfRangeException(); AbilityId = abilityId; CooldownTicks = cooldownTicks; ResourceCost = resourceCost; Range = range; MaxTargets = maxTargets; }
        public int AbilityId { get; }
        public long CooldownTicks { get; }
        public int ResourceCost { get; }
        public int Range { get; }
        public int MaxTargets { get; }
    }

    public readonly struct AbilityState
    {
        public AbilityState(long readyTick, int resource) { if (readyTick < 0 || resource < 0) throw new ArgumentOutOfRangeException(); ReadyTick = readyTick; Resource = resource; }
        public long ReadyTick { get; }
        public int Resource { get; }
        public bool CanUse(long tick, int cost) => tick >= ReadyTick && cost >= 0 && Resource >= cost;
        public AbilityState Consume(long tick, AbilityDescriptor descriptor) => new AbilityState(checked(tick + descriptor.CooldownTicks), Resource - descriptor.ResourceCost);
    }

    public readonly struct AbilityTargetCandidate : IComparable<AbilityTargetCandidate>
    {
        public AbilityTargetCandidate(EntityId target, int targetCount, int expectedValue, int incomingThreat, int healthDanger, bool friendlyFire, int opportunityCost)
        { Target = target; TargetCount = targetCount; ExpectedValue = expectedValue; IncomingThreat = incomingThreat; HealthDanger = healthDanger; FriendlyFire = friendlyFire; OpportunityCost = opportunityCost; }
        public EntityId Target { get; }
        public int TargetCount { get; }
        public int ExpectedValue { get; }
        public int IncomingThreat { get; }
        public int HealthDanger { get; }
        public bool FriendlyFire { get; }
        public int OpportunityCost { get; }
        public int Utility => checked(ExpectedValue + TargetCount + HealthDanger - IncomingThreat - OpportunityCost - (FriendlyFire ? 1000 : 0));
        public int CompareTo(AbilityTargetCandidate other) { int c = other.Utility.CompareTo(Utility); return c != 0 ? c : Target.CompareTo(other.Target); }
    }

    public readonly struct AbilityDecisionProfile
    {
        public AbilityDecisionProfile(bool autoEnabled, int minimumUtility, int maxCandidates) { if (minimumUtility < int.MinValue || maxCandidates <= 0) throw new ArgumentOutOfRangeException(); AutoEnabled = autoEnabled; MinimumUtility = minimumUtility; MaxCandidates = maxCandidates; }
        public bool AutoEnabled { get; }
        public int MinimumUtility { get; }
        public int MaxCandidates { get; }
    }

    public readonly struct AbilityUseProposal : IComparable<AbilityUseProposal>
    {
        public AbilityUseProposal(EntityId source, int abilityId, EntityId target, long tick, int utility, long ordinal)
        { Source = source; AbilityId = abilityId; Target = target; Tick = tick; Utility = utility; Ordinal = ordinal; }
        public EntityId Source { get; }
        public int AbilityId { get; }
        public EntityId Target { get; }
        public long Tick { get; }
        public int Utility { get; }
        public long Ordinal { get; }
        public int CompareTo(AbilityUseProposal other) { int c = Tick.CompareTo(other.Tick); if (c != 0) return c; c = Source.CompareTo(other.Source); return c != 0 ? c : Ordinal.CompareTo(other.Ordinal); }
    }

    public static class AbilityEvaluator
    {
        public static bool TryPropose(EntityId source, AbilityDescriptor descriptor, AbilityState state, AbilityDecisionProfile profile, ResolvedAutonomyProfile autonomy, IEnumerable<AbilityTargetCandidate> candidates, long tick, long ordinal, out AbilityUseProposal proposal, out CombatDiagnostic diagnostic)
        {
            proposal = default(AbilityUseProposal); diagnostic = null;
            if (!profile.AutoEnabled || !autonomy.Envelope.MayCast || (autonomy.Capabilities & AutonomyCapabilities.AutoCast) == 0) { diagnostic = new CombatDiagnostic(CombatDiagnosticCode.AbilityUnavailable, source, default(EntityId), tick, "Automatic ability use is not permitted."); return false; }
            if (!state.CanUse(tick, descriptor.ResourceCost)) { diagnostic = new CombatDiagnostic(CombatDiagnosticCode.AbilityCooldown, source, default(EntityId), tick, "Ability is unavailable or on cooldown."); return false; }
            var ordered = (candidates ?? Enumerable.Empty<AbilityTargetCandidate>()).Take(profile.MaxCandidates).Where(x => x.Target.IsValid && !x.FriendlyFire && x.TargetCount <= descriptor.MaxTargets && x.TargetCount > 0 && x.Utility >= profile.MinimumUtility).OrderBy(x => x).ToList();
            if (ordered.Count == 0) { diagnostic = new CombatDiagnostic(CombatDiagnosticCode.InvalidAbilityTarget, source, default(EntityId), tick, "No eligible ability target candidate."); return false; }
            AbilityTargetCandidate selected = ordered[0]; proposal = new AbilityUseProposal(source, descriptor.AbilityId, selected.Target, tick, selected.Utility, ordinal); return true;
        }
    }

    public readonly struct RetreatProposal : IComparable<RetreatProposal>
    {
        public RetreatProposal(EntityId entity, CellCoordinate safeCandidate, int threat, int healthDanger, int localSupport, long tick, long ordinal)
        { Entity = entity; SafeCandidate = safeCandidate; Threat = threat; HealthDanger = healthDanger; LocalSupport = localSupport; Tick = tick; Ordinal = ordinal; }
        public EntityId Entity { get; }
        public CellCoordinate SafeCandidate { get; }
        public int Threat { get; }
        public int HealthDanger { get; }
        public int LocalSupport { get; }
        public long Tick { get; }
        public long Ordinal { get; }
        public int Utility => checked(Threat + HealthDanger + LocalSupport);
        public int CompareTo(RetreatProposal other) { int c = Tick.CompareTo(other.Tick); if (c != 0) return c; c = Entity.CompareTo(other.Entity); return c != 0 ? c : Ordinal.CompareTo(other.Ordinal); }
    }

    public readonly struct CrushThreatCandidate
    {
        public CrushThreatCandidate(EntityId entity, int threat, CellCoordinate escape, bool capabilityAvailable) { Entity = entity; Threat = threat; Escape = escape; CapabilityAvailable = capabilityAvailable; }
        public EntityId Entity { get; }
        public int Threat { get; }
        public CellCoordinate Escape { get; }
        public bool CapabilityAvailable { get; }
        public bool ShouldEvade(ResolvedAutonomyProfile profile) => CapabilityAvailable && profile.Envelope.MayEvade && (profile.Capabilities & AutonomyCapabilities.Evade) != 0 && Threat > 0;
    }
}
