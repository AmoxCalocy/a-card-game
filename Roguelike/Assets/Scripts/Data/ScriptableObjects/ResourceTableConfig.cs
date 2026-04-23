using System.Collections.Generic;
using UnityEngine;

namespace OneManJourney.Data
{
    [CreateAssetMenu(fileName = "ResourceTableConfig", menuName = "OneManJourney/Data/Resource Table Config")]
    public sealed class ResourceTableConfig : ScriptableObject
    {
        [SerializeField] private string _id = "resource-table.default";
        [SerializeField] private string _displayName = "Default Resource Table";
        [SerializeField] private List<ResourceAmount> _startingResources = new List<ResourceAmount>();
        [Min(0)]
        [SerializeField] private int _startingCrisis;
        [Range(0, 100)]
        [SerializeField] private int _chapterDropDecayPercent = 10;

        public string Id => _id;
        public string DisplayName => _displayName;
        public IReadOnlyList<ResourceAmount> StartingResources => _startingResources;
        public int StartingCrisis => _startingCrisis;
        public int ChapterDropDecayPercent => _chapterDropDecayPercent;
    }
}
