using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneManJourney.Runtime
{
    public enum JourneyNodeType
    {
        Battle,
        Event,
        Supply,
        Boss
    }

    [Serializable]
    public sealed class JourneyMapGenerationConfig
    {
        [Min(6)]
        [SerializeField] private int _layerCount = 6;
        [Min(2)]
        [SerializeField] private int _lanesPerLayer = 3;
        [Min(0f)]
        [SerializeField] private float _battleWeight = 0.5f;
        [Min(0f)]
        [SerializeField] private float _eventWeight = 0.35f;
        [Min(0f)]
        [SerializeField] private float _supplyWeight = 0.15f;

        public int LayerCount => Mathf.Max(6, _layerCount);
        public int LanesPerLayer => Mathf.Max(2, _lanesPerLayer);
        public float BattleWeight => Mathf.Max(0f, _battleWeight);
        public float EventWeight => Mathf.Max(0f, _eventWeight);
        public float SupplyWeight => Mathf.Max(0f, _supplyWeight);

        public JourneyNodeType RollNodeType(System.Random random)
        {
            float total = BattleWeight + EventWeight + SupplyWeight;
            if (total <= Mathf.Epsilon)
            {
                return JourneyNodeType.Battle;
            }

            double roll = random.NextDouble() * total;
            if (roll < BattleWeight)
            {
                return JourneyNodeType.Battle;
            }

            roll -= BattleWeight;
            if (roll < EventWeight)
            {
                return JourneyNodeType.Event;
            }

            return JourneyNodeType.Supply;
        }
    }

    public sealed class JourneyMapNode
    {
        private readonly List<int> _nextNodeIds;

        public JourneyMapNode(int id, int layerIndex, int laneIndex, JourneyNodeType nodeType, IEnumerable<int> nextNodeIds)
        {
            Id = id;
            LayerIndex = layerIndex;
            LaneIndex = laneIndex;
            NodeType = nodeType;
            _nextNodeIds = nextNodeIds == null ? new List<int>() : new List<int>(nextNodeIds);
        }

        public int Id { get; }
        public int LayerIndex { get; }
        public int LaneIndex { get; }
        public JourneyNodeType NodeType { get; }
        public IReadOnlyList<int> NextNodeIds => _nextNodeIds;
    }

    public sealed class JourneyMap
    {
        private readonly List<JourneyMapNode> _nodes;
        private readonly Dictionary<JourneyNodeType, int> _typeCounts;

        public JourneyMap(int seed, List<JourneyMapNode> nodes, int routeCount, int branchingNodeCount)
        {
            Seed = seed;
            _nodes = nodes ?? new List<JourneyMapNode>();
            RouteCount = Mathf.Max(0, routeCount);
            BranchingNodeCount = Mathf.Max(0, branchingNodeCount);
            _typeCounts = BuildTypeCounts(_nodes);
        }

        public int Seed { get; }
        public int RouteCount { get; }
        public int BranchingNodeCount { get; }
        public IReadOnlyList<JourneyMapNode> Nodes => _nodes;

        public int GetTypeCount(JourneyNodeType nodeType)
        {
            return _typeCounts.TryGetValue(nodeType, out int count) ? count : 0;
        }

        private static Dictionary<JourneyNodeType, int> BuildTypeCounts(IReadOnlyList<JourneyMapNode> nodes)
        {
            var counts = new Dictionary<JourneyNodeType, int>();
            for (int i = 0; i < nodes.Count; i++)
            {
                JourneyNodeType nodeType = nodes[i].NodeType;
                counts[nodeType] = counts.TryGetValue(nodeType, out int count) ? count + 1 : 1;
            }

            return counts;
        }
    }
}
