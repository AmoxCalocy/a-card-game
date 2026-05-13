using System.Collections.Generic;
using OneManJourney.Data;

namespace OneManJourney.Runtime
{
    public sealed class BattleEncounterConfig
    {
        private readonly List<EnemyConfig> _enemyQueue;

        public BattleEncounterConfig(int nodeId, JourneyNodeType nodeType, int encounterSeed, IEnumerable<EnemyConfig> enemyQueue)
        {
            NodeId = nodeId;
            NodeType = nodeType;
            EncounterSeed = encounterSeed;
            _enemyQueue = enemyQueue == null ? new List<EnemyConfig>() : new List<EnemyConfig>(enemyQueue);
        }

        public int NodeId { get; }
        public JourneyNodeType NodeType { get; }
        public int EncounterSeed { get; }
        public IReadOnlyList<EnemyConfig> EnemyQueue => _enemyQueue;
    }
}
