using UnityEngine;

namespace OneManJourney.Data
{
    [CreateAssetMenu(fileName = "RelicConfig", menuName = "OneManJourney/Data/Relic Config")]
    public sealed class RelicConfig : ScriptableObject
    {
        [SerializeField] private string _id = "relic.id";
        [SerializeField] private string _displayName = "New Relic";
        [TextArea(2, 4)]
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private RarityTier _rarity = RarityTier.Uncommon;
        [SerializeField] private RelicTriggerType _trigger = RelicTriggerType.Passive;
        [SerializeField] private RelicModifierType _modifier = RelicModifierType.ResourceGain;
        [SerializeField] private ResourceType _resourceType = ResourceType.Wealth;
        [SerializeField] private StatusEffectType _statusEffect = StatusEffectType.None;
        [SerializeField] private int _magnitude = 1;
        [SerializeField] private bool _isConsumable;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public RarityTier Rarity => _rarity;
        public RelicTriggerType Trigger => _trigger;
        public RelicModifierType Modifier => _modifier;
        public ResourceType ResourceType => _resourceType;
        public StatusEffectType StatusEffect => _statusEffect;
        public int Magnitude => _magnitude;
        public bool IsConsumable => _isConsumable;
    }
}
