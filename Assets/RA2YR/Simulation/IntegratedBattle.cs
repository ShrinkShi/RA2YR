using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RA2YR.Simulation
{
    public readonly struct BattleUnitState : IComparable<BattleUnitState>
    {
        public BattleUnitState(EntityId entity, int owner, int x, int y, int health, int range, int damage, long cooldownReady)
        { Entity = entity; Owner = owner; X = x; Y = y; Health = health; Range = range; Damage = damage; CooldownReady = cooldownReady; }
        public EntityId Entity { get; }
        public int Owner { get; }
        public int X { get; }
        public int Y { get; }
        public int Health { get; }
        public int Range { get; }
        public int Damage { get; }
        public long CooldownReady { get; }
        public int CompareTo(BattleUnitState other) => Entity.CompareTo(other.Entity);
    }

    public sealed class IntegratedBattleResult
    {
        internal IntegratedBattleResult(long tick, IEnumerable<BattleUnitState> units, string hash, int attacks, int deaths)
        { Tick = tick; Units = new ReadOnlyCollection<BattleUnitState>((units ?? Enumerable.Empty<BattleUnitState>()).OrderBy(x => x).ToList()); StateHash = hash ?? string.Empty; AttackCount = attacks; DeathCount = deaths; }
        public long Tick { get; }
        public IReadOnlyList<BattleUnitState> Units { get; }
        public string StateHash { get; }
        public int AttackCount { get; }
        public int DeathCount { get; }
    }

    public sealed class IntegratedSyntheticBattle
    {
        private readonly int width;
        private readonly int height;
        private readonly int maxUnits;
        private readonly List<BattleUnitState> units = new List<BattleUnitState>();
        private long tick;
        public IntegratedSyntheticBattle(int width = 16, int height = 16, int maxUnits = 128) { if (width <= 0 || height <= 0 || maxUnits <= 0) throw new ArgumentOutOfRangeException(); this.width = width; this.height = height; this.maxUnits = maxUnits; }
        public IReadOnlyList<BattleUnitState> Units => new ReadOnlyCollection<BattleUnitState>(units.OrderBy(x => x).ToList());
        public void Add(int owner, int x, int y, int health = 10, int range = 4, int damage = 2) { if (units.Count >= maxUnits || owner < 0 || x < 0 || y < 0 || x >= width || y >= height || health <= 0 || range < 0 || damage < 0) throw new ArgumentOutOfRangeException(); units.Add(new BattleUnitState(new EntityId(units.Count, 1), owner, x, y, health, range, damage, 0)); }
        public IntegratedBattleResult Step(bool allowAutonomy = true)
        {
            var proposals = new List<DamageEvent>(); int attacks = 0;
            foreach (BattleUnitState source in units.OrderBy(x => x))
            {
                if (source.Health <= 0 || source.CooldownReady > tick) continue;
                BattleUnitState target = units.Where(x => x.Health > 0 && x.Owner != source.Owner).OrderBy(x => Math.Abs(x.X - source.X) + Math.Abs(x.Y - source.Y)).ThenBy(x => x.Entity).FirstOrDefault();
                if (!target.Entity.IsValid) continue;
                int distance = Math.Abs(target.X - source.X) + Math.Abs(target.Y - source.Y); if (distance > source.Range) continue;
                proposals.Add(new DamageEvent(source.Entity, target.Entity, source.Damage, 0, tick, attacks++));
            }
            proposals.Sort(); int deaths = 0;
            foreach (DamageEvent e in proposals)
            {
                int index = units.FindIndex(x => x.Entity == e.Target); if (index < 0 || units[index].Health <= 0) continue;
                BattleUnitState current = units[index]; int next = Math.Max(0, current.Health - e.Amount); if (next == 0 && current.Health > 0) deaths++;
                units[index] = new BattleUnitState(current.Entity, current.Owner, current.X, current.Y, next, current.Range, current.Damage, checked(tick + 1));
            }
            tick = checked(tick + 1);
            return new IntegratedBattleResult(tick, units, CanonicalHash(), attacks, deaths);
        }
        public string CanonicalHash() { var values = units.OrderBy(x => x.Owner).ThenBy(x => x.X).ThenBy(x => x.Y).ThenBy(x => x.Health).Select(x => x.Owner + ":" + x.X + ":" + x.Y + ":" + x.Health + ":" + x.CooldownReady); using (var sha = System.Security.Cryptography.SHA256.Create()) return string.Concat(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("|", values))).Select(x => x.ToString("x2"))); }
    }
}
