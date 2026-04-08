using System.Collections.Generic;
using UnityEngine;

namespace OneManJourney.Data
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "OneManJourney/Data/Enemy Config")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [SerializeField] private string _id = "enemy.id";
        [SerializeField] private string _displayName = "New Enemy";
        [TextArea(2, 4)]
        [SerializeField] private string _description = string.Empty;
        [Min(1)]
        [SerializeField] private int _maxHealth = 20;
        [Min(0)]
        [SerializeField] private int _baseAttack = 6;
        [Min(0)]
        [SerializeField] private int _baseDefense;
        [SerializeField] private EnemyIntentType _primaryIntent = EnemyIntentType.Attack;
        [SerializeField] private List<ResourceAmount> _defeatRewards = new List<ResourceAmount>();

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public int MaxHealth => _maxHealth;
        public int BaseAttack => _baseAttack;
        public int BaseDefense => _baseDefense;
        public EnemyIntentType PrimaryIntent => _primaryIntent;
        public IReadOnlyList<ResourceAmount> DefeatRewards => _defeatRewards;
    }
}
