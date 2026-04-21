using UnityEngine;

namespace OneManJourney.Data
{
    [CreateAssetMenu(fileName = "CardConfig", menuName = "OneManJourney/Data/Card Config")]
    public sealed class CardConfig : ScriptableObject
    {
        [SerializeField] private string _id = "card.id";
        [SerializeField] private string _displayName = "New Card";
        [TextArea(2, 4)]
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private CardType _cardType = CardType.Attack;
        [SerializeField] private RarityTier _rarity = RarityTier.Common;
        [Min(0)]
        [SerializeField] private int _energyCost = 1;
        [SerializeField] private int _baseValue;
        [SerializeField] private StatusEffectType _statusEffect = StatusEffectType.None;
        [Min(0)]
        [SerializeField] private int _statusStacks;
        [SerializeField] private bool _exhaustOnPlay;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public CardType CardType => _cardType;
        public RarityTier Rarity => _rarity;
        public int EnergyCost => _energyCost;
        public int BaseValue => _baseValue;
        public StatusEffectType StatusEffect => _statusEffect;
        public int StatusStacks => _statusStacks;
        public bool ExhaustOnPlay => _exhaustOnPlay;
    }
}
