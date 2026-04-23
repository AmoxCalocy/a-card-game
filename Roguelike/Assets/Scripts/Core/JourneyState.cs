using System;
using UnityEngine;

namespace OneManJourney.Runtime
{
    [Serializable]
    public sealed class JourneyState
    {
        [SerializeField] private int _chapter = 1;
        [SerializeField] private int _nodeIndex;
        [SerializeField] private int _nodesVisited;
        [SerializeField] private int _runSeed;
        [SerializeField] private int _crisisValue;

        public int Chapter
        {
            get => _chapter;
            set => _chapter = Mathf.Max(1, value);
        }

        public int NodeIndex
        {
            get => _nodeIndex;
            set => _nodeIndex = Mathf.Max(0, value);
        }

        public int NodesVisited
        {
            get => _nodesVisited;
            set => _nodesVisited = Mathf.Max(0, value);
        }

        public int RunSeed
        {
            get => _runSeed;
            set => _runSeed = value;
        }

        public int CrisisValue
        {
            get => _crisisValue;
            set => _crisisValue = value;
        }

        public static JourneyState CreateDefault()
        {
            return new JourneyState
            {
                _chapter = 1,
                _nodeIndex = 0,
                _nodesVisited = 0,
                _runSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                _crisisValue = 0
            };
        }
    }
}
