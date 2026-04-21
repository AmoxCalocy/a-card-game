using System.Collections.Generic;
using UnityEngine;

namespace OneManJourney.Data
{
    [CreateAssetMenu(fileName = "CompanionConfig", menuName = "OneManJourney/Data/Companion Config")]
    public sealed class CompanionConfig : ScriptableObject
    {
        [SerializeField] private string _id = "companion.id";
        [SerializeField] private string _displayName = "New Companion";
        [TextArea(2, 4)]
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private CompanionRole _role = CompanionRole.Support;
        [Min(1)]
        [SerializeField] private int _maxHealth = 30;
        [Range(0, 100)]
        [SerializeField] private int _startingLoyalty = 60;
        [SerializeField] private int _skillCheckBonus;
        [SerializeField] private List<string> _traitIds = new List<string>();
        [SerializeField] private List<CardConfig> _starterCards = new List<CardConfig>();

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public CompanionRole Role => _role;
        public int MaxHealth => _maxHealth;
        public int StartingLoyalty => _startingLoyalty;
        public int SkillCheckBonus => _skillCheckBonus;
        public IReadOnlyList<string> TraitIds => _traitIds;
        public IReadOnlyList<CardConfig> StarterCards => _starterCards;
    }
}
