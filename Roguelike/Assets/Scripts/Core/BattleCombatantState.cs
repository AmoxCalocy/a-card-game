using System;
using System.Collections.Generic;
using System.Text;
using OneManJourney.Data;

namespace OneManJourney.Runtime
{
    public sealed class BattleCombatantState
    {
        private readonly Dictionary<StatusEffectType, int> _statusStacks = new Dictionary<StatusEffectType, int>();

        public BattleCombatantState(string id, string displayName, int maxHealth, int initialArmor, EnemyConfig enemyConfig = null)
        {
            Id = string.IsNullOrWhiteSpace(id) ? "combatant" : id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
            MaxHealth = maxHealth < 1 ? 1 : maxHealth;
            CurrentHealth = MaxHealth;
            Armor = initialArmor < 0 ? 0 : initialArmor;
            EnemyConfig = enemyConfig;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public int Armor { get; private set; }
        public EnemyConfig EnemyConfig { get; }
        public bool IsDefeated => CurrentHealth <= 0;

        public int ApplyDamage(int amount)
        {
            if (amount <= 0 || IsDefeated)
            {
                return 0;
            }

            int remaining = amount;
            if (Armor > 0)
            {
                int absorbed = remaining <= Armor ? remaining : Armor;
                Armor -= absorbed;
                remaining -= absorbed;
            }

            if (remaining <= 0)
            {
                return 0;
            }

            int previousHealth = CurrentHealth;
            CurrentHealth -= remaining;
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }

            return previousHealth - CurrentHealth;
        }

        public int AddArmor(int amount)
        {
            if (amount <= 0 || IsDefeated)
            {
                return 0;
            }

            Armor += amount;
            return amount;
        }

        public int Heal(int amount)
        {
            if (amount <= 0 || IsDefeated)
            {
                return 0;
            }

            int previousHealth = CurrentHealth;
            CurrentHealth += amount;
            if (CurrentHealth > MaxHealth)
            {
                CurrentHealth = MaxHealth;
            }

            return CurrentHealth - previousHealth;
        }

        public int AddStatus(StatusEffectType statusEffect, int stacks)
        {
            if (statusEffect == StatusEffectType.None || stacks <= 0 || IsDefeated)
            {
                return 0;
            }

            int existing = GetStatusStacks(statusEffect);
            int next = existing + stacks;
            _statusStacks[statusEffect] = next;
            return stacks;
        }

        public int GetStatusStacks(StatusEffectType statusEffect)
        {
            if (statusEffect == StatusEffectType.None)
            {
                return 0;
            }

            return _statusStacks.TryGetValue(statusEffect, out int stacks) ? stacks : 0;
        }

        public string GetStatusSummary()
        {
            if (_statusStacks.Count == 0)
            {
                return "none";
            }

            var builder = new StringBuilder();
            bool hasAny = false;
            foreach (KeyValuePair<StatusEffectType, int> entry in _statusStacks)
            {
                if (entry.Value <= 0)
                {
                    continue;
                }

                if (hasAny)
                {
                    builder.Append(", ");
                }

                builder.Append(entry.Key);
                builder.Append('x');
                builder.Append(entry.Value);
                hasAny = true;
            }

            return hasAny ? builder.ToString() : "none";
        }
    }
}
