using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Simulation
{
    public readonly struct PlayerId : IEquatable<PlayerId>, IComparable<PlayerId>
    { public PlayerId(int value) { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); Value = value; } public int Value { get; } public bool Equals(PlayerId o) => Value == o.Value; public override bool Equals(object o) => o is PlayerId && Equals((PlayerId)o); public override int GetHashCode() => Value; public int CompareTo(PlayerId o) => Value.CompareTo(o.Value); }
    public readonly struct HouseRuntimeState { public HouseRuntimeState(PlayerId player, string rawName, int country, bool defeated = false) { Player = player; RawName = rawName ?? string.Empty; Country = country; Defeated = defeated; } public PlayerId Player { get; } public string RawName { get; } public int Country { get; } public bool Defeated { get; } public HouseRuntimeState WithDefeat(bool value) => new HouseRuntimeState(Player, RawName, Country, value); }
    public readonly struct AllianceRuntimeState : IComparable<AllianceRuntimeState> { public AllianceRuntimeState(PlayerId source, PlayerId target, bool authored) { Source = source; Target = target; Authored = authored; } public PlayerId Source { get; } public PlayerId Target { get; } public bool Authored { get; } public int CompareTo(AllianceRuntimeState o) { int c = Source.CompareTo(o.Source); return c != 0 ? c : Target.CompareTo(o.Target); } }
    public readonly struct PowerState { public PowerState(int produced, int consumed) { Produced = produced; Consumed = consumed; } public int Produced { get; } public int Consumed { get; } public int Deficit => Math.Max(0, Consumed - Produced); public bool LowPower => Deficit > 0; }
    public readonly struct TechnologySnapshot { public TechnologySnapshot(int techLevel, IEnumerable<string> capabilities) { if (techLevel < 0) throw new ArgumentOutOfRangeException(nameof(techLevel)); TechLevel = techLevel; Capabilities = new ReadOnlyCollection<string>((capabilities ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList()); } public int TechLevel { get; } public IReadOnlyList<string> Capabilities { get; } }
    public readonly struct OwnershipSnapshot { public OwnershipSnapshot(PlayerId owner, string type) { Owner = owner; Type = type ?? string.Empty; } public PlayerId Owner { get; } public string Type { get; } }
    public readonly struct EconomyTransactionId : IComparable<EconomyTransactionId> { public EconomyTransactionId(long value) { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); Value = value; } public long Value { get; } public int CompareTo(EconomyTransactionId o) => Value.CompareTo(o.Value); }
    public enum EconomyTransactionSource { StartingCredits, HarvestIncome, ProductionSpend, RepairSpend, SellRefund, ScriptAdjustment }
    public readonly struct EconomyTransaction : IComparable<EconomyTransaction> { public EconomyTransaction(EconomyTransactionId id, PlayerId player, long tick, EconomyTransactionSource source, long amount, string reason) { if (amount == 0) throw new ArgumentOutOfRangeException(nameof(amount)); Id = id; Player = player; Tick = tick; Source = source; Amount = amount; Reason = reason ?? string.Empty; } public EconomyTransactionId Id { get; } public PlayerId Player { get; } public long Tick { get; } public EconomyTransactionSource Source { get; } public long Amount { get; } public string Reason { get; } public int CompareTo(EconomyTransaction o) { int c = Tick.CompareTo(o.Tick); return c != 0 ? c : Id.CompareTo(o.Id); } }
    public readonly struct CreditAccount { public CreditAccount(PlayerId player, long balance) { Player = player; Balance = balance; } public PlayerId Player { get; } public long Balance { get; } }
    public sealed class EconomyAuthority
    {
        private readonly Dictionary<PlayerId, CreditAccount> accounts = new Dictionary<PlayerId, CreditAccount>(); private readonly List<EconomyTransaction> transactions = new List<EconomyTransaction>(); private long nextId;
        public void Register(PlayerId player, long startingCredits) { if (startingCredits < 0) throw new ArgumentOutOfRangeException(nameof(startingCredits)); accounts[player] = new CreditAccount(player, startingCredits); }
        public CreditAccount Get(PlayerId player) => accounts.TryGetValue(player, out CreditAccount a) ? a : throw new KeyNotFoundException();
        public bool TryApply(EconomyTransactionSource source, PlayerId player, long tick, long amount, string reason, out EconomyTransaction transaction) { transaction = new EconomyTransaction(new EconomyTransactionId(nextId++), player, tick, source, amount, reason); CreditAccount current; if (!accounts.TryGetValue(player, out current)) return false; long next; try { next = checked(current.Balance + amount); } catch (OverflowException) { return false; } if (next < 0) return false; accounts[player] = new CreditAccount(player, next); transactions.Add(transaction); return true; }
        public IReadOnlyList<EconomyTransaction> Transactions => new ReadOnlyCollection<EconomyTransaction>(transactions.OrderBy(x => x).ToList());
        public string StateHash() { var s = string.Join("|", accounts.OrderBy(x => x.Key).Select(x => x.Key.Value + ":" + x.Value.Balance)); using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(s)).Select(x => x.ToString("x2"))); }
    }
}
