using System.Collections.Generic;
using OneManJourney.Data;
using UnityEngine;

namespace OneManJourney.Runtime
{
    public sealed class CompanionState
    {
        public const int LoyaltySafeThreshold = 60;
        public const int LoyaltyWarningThreshold = 30;
        public const int LoyaltyCriticalThreshold = 1;

        private readonly CompanionConfig _config;
        private int _currentLoyalty;
        private int _currentHealth;
        private bool _isInjured;

        public CompanionState(CompanionConfig config)
        {
            _config = config;
            _currentLoyalty = config.StartingLoyalty;
            _currentHealth = config.MaxHealth;
            _isInjured = false;
        }

        public CompanionConfig Config => _config;
        public string Id => _config.Id;
        public string DisplayName => _config.DisplayName;
        public CompanionRole Role => _config.Role;
        public int MaxHealth => _config.MaxHealth;
        public int CurrentHealth
        {
            get => _currentHealth;
            set => _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
        }

        public int CurrentLoyalty
        {
            get => _currentLoyalty;
            set => _currentLoyalty = Mathf.Clamp(value, 0, 100);
        }

        public bool IsInjured
        {
            get => _isInjured;
            set
            {
                _isInjured = value;
                if (!_isInjured)
                {
                    _currentHealth = MaxHealth;
                }
            }
        }

        public int SkillCheckBonus => _config.SkillCheckBonus;
        public IReadOnlyList<string> TraitIds => _config.TraitIds;
        public IReadOnlyList<CardConfig> StarterCards => _config.StarterCards;

        public bool IsLoyaltySafe => _currentLoyalty >= LoyaltySafeThreshold;
        public bool IsLoyaltyWarning => _currentLoyalty >= LoyaltyWarningThreshold && _currentLoyalty < LoyaltySafeThreshold;
        public bool IsLoyaltyCritical => _currentLoyalty >= LoyaltyCriticalThreshold && _currentLoyalty < LoyaltyWarningThreshold;
        public bool ShouldAutoDepart => _currentLoyalty <= 0;

        public float DepartureRisk
        {
            get
            {
                if (_currentLoyalty >= LoyaltySafeThreshold) return 0f;
                if (_currentLoyalty >= LoyaltyWarningThreshold) return 0.15f;
                if (_currentLoyalty >= LoyaltyCriticalThreshold) return 0.40f;
                return 1f;
            }
        }

        public int ModifyLoyalty(int delta)
        {
            int previous = _currentLoyalty;
            CurrentLoyalty = _currentLoyalty + delta;
            return _currentLoyalty - previous;
        }

        public int GetSkillCheckValue()
        {
            float loyaltyModifier = 0f;
            if (_currentLoyalty >= LoyaltySafeThreshold) loyaltyModifier = 2f;
            else if (_currentLoyalty >= LoyaltyWarningThreshold) loyaltyModifier = 0f;
            else if (_currentLoyalty >= LoyaltyCriticalThreshold) loyaltyModifier = -2f;
            else loyaltyModifier = -5f;

            return Mathf.RoundToInt(SkillCheckBonus + loyaltyModifier);
        }

        public string GetLoyaltyLabel()
        {
            if (_currentLoyalty >= LoyaltySafeThreshold) return "Loyal";
            if (_currentLoyalty >= LoyaltyWarningThreshold) return "Uneasy";
            if (_currentLoyalty >= LoyaltyCriticalThreshold) return "Discontent";
            return "Rebellious";
        }

        public string GetStatusSummary()
        {
            string injury = _isInjured ? " [Injured]" : string.Empty;
            return $"{DisplayName} ({Role}) HP:{_currentHealth}/{MaxHealth} Loyalty:{_currentLoyalty} ({GetLoyaltyLabel()}){injury}";
        }
    }
}
