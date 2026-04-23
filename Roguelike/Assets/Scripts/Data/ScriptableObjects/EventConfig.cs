using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneManJourney.Data
{
    [CreateAssetMenu(fileName = "EventConfig", menuName = "OneManJourney/Data/Event Config")]
    public sealed class EventConfig : ScriptableObject
    {
        [SerializeField] private string _id = "event.id";
        [SerializeField] private string _displayName = "New Event";
        [TextArea(2, 5)]
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private List<EventOptionData> _options = new List<EventOptionData>();

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public IReadOnlyList<EventOptionData> Options => _options;
    }

    [Serializable]
    public sealed class EventOptionData
    {
        [SerializeField] private string _optionId = "option.id";
        [SerializeField] private string _title = "New Option";
        [TextArea(2, 4)]
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private EventResolutionType _resolutionType = EventResolutionType.Combat;
        [Range(0f, 1f)]
        [SerializeField] private float _successChance = 1f;
        [SerializeField] private List<ResourceAmount> _costs = new List<ResourceAmount>();
        [SerializeField] private List<ResourceAmount> _rewards = new List<ResourceAmount>();
        [SerializeField] private int _requiredReputation;
        [Min(0)]
        [SerializeField] private int _sacrificeCardCount;
        [SerializeField] private CompanionConfig _recruitedCompanion;

        public string OptionId => _optionId;
        public string Title => _title;
        public string Description => _description;
        public EventResolutionType ResolutionType => _resolutionType;
        public float SuccessChance => _successChance;
        public IReadOnlyList<ResourceAmount> Costs => _costs;
        public IReadOnlyList<ResourceAmount> Rewards => _rewards;
        public int RequiredReputation => _requiredReputation;
        public int SacrificeCardCount => _sacrificeCardCount;
        public CompanionConfig RecruitedCompanion => _recruitedCompanion;
    }
}
