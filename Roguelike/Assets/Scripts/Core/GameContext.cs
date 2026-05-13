using System;
using System.Collections.Generic;
using OneManJourney.Data;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameContext : MonoBehaviour
    {
        [Header("Initial Data (Optional)")]
        [SerializeField] private ResourceTableConfig _resourceTable;
        [SerializeField] private List<CardConfig> _startingCardPool = new List<CardConfig>();
        [SerializeField] private List<EventConfig> _startingEventPool = new List<EventConfig>();
        [SerializeField] private List<EnemyConfig> _startingEnemyPool = new List<EnemyConfig>();
        [SerializeField] private bool _autoLoadEditorData = true;
        [Header("Journey Map")]
        [SerializeField] private JourneyMapGenerationConfig _journeyMapGenerationConfig = new JourneyMapGenerationConfig();
        [Header("Battle Entry")]
        [Min(1)]
        [SerializeField] private int _battleEnemyCountMin = 1;
        [Min(1)]
        [SerializeField] private int _battleEnemyCountMax = 3;
        [Min(1)]
        [SerializeField] private int _bossEnemyCountMin = 2;
        [Min(1)]
        [SerializeField] private int _bossEnemyCountMax = 4;
        [Header("Journey Progress")]
        [Min(1)]
        [SerializeField] private int _foodCostPerAdvance = 1;
        [SerializeField] private string _battleSceneName = "BattleScene";
        [SerializeField] private string _eventSceneName = "EventScene";
        [SerializeField] private string _supplySceneName = "SupplyScene";
        [SerializeField] private string _bossSceneName = "BossScene";
        [Header("Crisis")]
        [Min(0)]
        [SerializeField] private int _crisisGainPerAdvance = 1;
        [Min(1)]
        [SerializeField] private int _disasterTriggerThreshold = 6;
        [Min(1)]
        [SerializeField] private int _disasterTriggerStep = 6;

        private readonly Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();
        private readonly List<CardConfig> _cardPool = new List<CardConfig>();
        private readonly List<EventConfig> _eventPool = new List<EventConfig>();
        private readonly List<EnemyConfig> _enemyPool = new List<EnemyConfig>();
        private readonly Dictionary<int, BattleEncounterConfig> _battleNodeEncounterConfigs = new Dictionary<int, BattleEncounterConfig>();
        private GameEventBus _eventBus;
        private JourneyMap _journeyMap;
        private bool _isInitialized;
        private int _activeJourneyNodeId = -1;
        private JourneyNodeType _activeJourneyNodeType = JourneyNodeType.Battle;
        private string _activeJourneySceneName = string.Empty;
        private string _lastJourneyAdvanceBlockMessage = string.Empty;
        private EventConfig _pendingDisasterEvent;
        private DisasterEventType _pendingDisasterType = DisasterEventType.None;
        private string _lastDisasterTriggerMessage = string.Empty;
        private int _nextDisasterTriggerThreshold;
        private BattleEncounterConfig _activeBattleEncounterConfig;

        public static GameContext Instance { get; private set; }

        public event Action Initialized;
        public event Action StateChanged;

        public ResourceTableConfig ResourceTable => _resourceTable;
        public IReadOnlyDictionary<ResourceType, int> Resources => _resources;
        public IReadOnlyList<CardConfig> CardPool => _cardPool;
        public IReadOnlyList<EventConfig> EventPool => _eventPool;
        public IReadOnlyList<EnemyConfig> EnemyPool => _enemyPool;
        public GameEventBus EventBus => _eventBus;
        public JourneyMap JourneyMap => _journeyMap;
        public int FoodCostPerAdvance => Mathf.Max(1, _foodCostPerAdvance);
        public bool HasActiveJourneyEncounter => _activeJourneyNodeId >= 0;
        public int ActiveJourneyNodeId => _activeJourneyNodeId;
        public JourneyNodeType ActiveJourneyNodeType => _activeJourneyNodeType;
        public string ActiveJourneySceneName => _activeJourneySceneName;
        public string LastJourneyAdvanceBlockMessage => _lastJourneyAdvanceBlockMessage;
        public int CrisisGainPerAdvance => Mathf.Max(0, _crisisGainPerAdvance);
        public int DisasterTriggerThreshold => Mathf.Max(1, _disasterTriggerThreshold);
        public int DisasterTriggerStep => Mathf.Max(1, _disasterTriggerStep);
        public int NextDisasterTriggerThreshold => _nextDisasterTriggerThreshold <= 0 ? DisasterTriggerThreshold : _nextDisasterTriggerThreshold;
        public EventConfig PendingDisasterEvent => _pendingDisasterEvent;
        public DisasterEventType PendingDisasterType => _pendingDisasterType;
        public string LastDisasterTriggerMessage => _lastDisasterTriggerMessage;
        public BattleEncounterConfig ActiveBattleEncounterConfig => _activeBattleEncounterConfig;
        // Avoid Unity API calls during MonoBehaviour construction/field initialization.
        public JourneyState JourneyState { get; private set; } = new JourneyState();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameServices.Register(this);
            _eventBus = new GameEventBus();
            GameServices.Register(_eventBus);
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            GameServices.Unregister<GameContext>();
            GameServices.Unregister<GameEventBus>();
            _eventBus?.Clear();
            _eventBus = null;
            Instance = null;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            LoadDefaultsIfNeeded();
            BuildResources();
            BuildPools();

            JourneyState = JourneyState.CreateDefault();
            JourneyState.CrisisValue = GetResource(ResourceType.Crisis);
            BuildJourneyMap(JourneyState.RunSeed);
            ResetJourneyEncounterState();
            ClearJourneyAdvanceBlockMessage();
            ResetDisasterState();

            _isInitialized = true;
            Initialized?.Invoke();
            Publish(new GameContextInitializedEvent(
                JourneyState.Chapter,
                JourneyState.NodeIndex,
                JourneyState.NodesVisited,
                JourneyState.CrisisValue,
                _cardPool.Count,
                _eventPool.Count));
            Publish(new JourneyMapGeneratedEvent(_journeyMap));
            NotifyStateChanged();
        }

        public int GetResource(ResourceType type)
        {
            return _resources.TryGetValue(type, out int amount) ? amount : 0;
        }

        public void SetResource(ResourceType type, int amount)
        {
            int previousAmount = GetResource(type);
            if (type != ResourceType.Crisis && amount < 0)
            {
                amount = 0;
            }

            if (previousAmount == amount)
            {
                return;
            }

            _resources[type] = amount;
            if (type == ResourceType.Crisis)
            {
                JourneyState.CrisisValue = amount;
                EvaluateDisasterTrigger(previousAmount, amount);
            }

            Publish(new ResourceChangedEvent(type, previousAmount, amount));
            NotifyStateChanged();
        }

        public void AddResource(ResourceType type, int delta)
        {
            int next = GetResource(type) + delta;
            SetResource(type, next);
        }

        public void SetJourneyProgress(int chapter, int nodeIndex, int nodesVisited)
        {
            int previousChapter = JourneyState.Chapter;
            int previousNodeIndex = JourneyState.NodeIndex;
            int previousNodesVisited = JourneyState.NodesVisited;

            JourneyState.Chapter = chapter;
            JourneyState.NodeIndex = nodeIndex;
            JourneyState.NodesVisited = nodesVisited;

            if (previousChapter == JourneyState.Chapter
                && previousNodeIndex == JourneyState.NodeIndex
                && previousNodesVisited == JourneyState.NodesVisited)
            {
                return;
            }

            Publish(new NodeSelectedEvent(
                previousChapter,
                previousNodeIndex,
                previousNodesVisited,
                JourneyState.Chapter,
                JourneyState.NodeIndex,
                JourneyState.NodesVisited));
            NotifyStateChanged();
        }

        public void AdvanceNode()
        {
            if (!TryGetCurrentJourneyNode(out JourneyMapNode currentNode) || currentNode.NextNodeIds.Count == 0)
            {
                return;
            }

            if (!TryEnterNextJourneyNode(currentNode.NextNodeIds[0], out _))
            {
                return;
            }

            TryCompleteActiveJourneyNode(out _);
        }

        public bool TryGetJourneyNode(int nodeId, out JourneyMapNode node)
        {
            if (_journeyMap == null)
            {
                node = null;
                return false;
            }

            return _journeyMap.TryGetNode(nodeId, out node);
        }

        public bool TryGetCurrentJourneyNode(out JourneyMapNode node)
        {
            return TryGetJourneyNode(JourneyState.NodeIndex, out node);
        }

        public bool TryGetBattleNodeEncounterConfig(int nodeId, out BattleEncounterConfig encounterConfig)
        {
            return _battleNodeEncounterConfigs.TryGetValue(nodeId, out encounterConfig);
        }

        public IReadOnlyList<JourneyMapNode> GetAvailableNextJourneyNodes()
        {
            var nextNodes = new List<JourneyMapNode>();
            if (!TryGetCurrentJourneyNode(out JourneyMapNode currentNode))
            {
                return nextNodes;
            }

            IReadOnlyList<int> nextNodeIds = currentNode.NextNodeIds;
            for (int i = 0; i < nextNodeIds.Count; i++)
            {
                int nodeId = nextNodeIds[i];
                if (TryGetJourneyNode(nodeId, out JourneyMapNode nextNode))
                {
                    nextNodes.Add(nextNode);
                }
            }

            return nextNodes;
        }

        public bool TryEnterNextJourneyNode(int targetNodeId, out string blockMessage)
        {
            if (HasActiveJourneyEncounter)
            {
                return BlockJourneyAdvance(
                    JourneyAdvanceBlockReason.EncounterAlreadyActive,
                    "A node encounter is already active. Complete it before selecting another path.",
                    out blockMessage);
            }

            if (_journeyMap == null)
            {
                return BlockJourneyAdvance(
                    JourneyAdvanceBlockReason.MissingMap,
                    "Journey map is not generated yet.",
                    out blockMessage);
            }

            if (!TryGetCurrentJourneyNode(out JourneyMapNode currentNode))
            {
                return BlockJourneyAdvance(
                    JourneyAdvanceBlockReason.MissingCurrentNode,
                    "Current journey node is missing from the map.",
                    out blockMessage);
            }

            if (GetResource(ResourceType.Food) <= 0)
            {
                return BlockJourneyAdvance(
                    JourneyAdvanceBlockReason.InsufficientFood,
                    "Food is depleted. You cannot advance to the next node.",
                    out blockMessage);
            }

            bool isConnected = false;
            for (int i = 0; i < currentNode.NextNodeIds.Count; i++)
            {
                if (currentNode.NextNodeIds[i] == targetNodeId)
                {
                    isConnected = true;
                    break;
                }
            }

            if (!isConnected)
            {
                return BlockJourneyAdvance(
                    JourneyAdvanceBlockReason.InvalidPath,
                    $"Node {targetNodeId} is not connected to current node {currentNode.Id}.",
                    out blockMessage);
            }

            if (!TryGetJourneyNode(targetNodeId, out JourneyMapNode targetNode))
            {
                return BlockJourneyAdvance(
                    JourneyAdvanceBlockReason.MissingTargetNode,
                    $"Target node {targetNodeId} does not exist.",
                    out blockMessage);
            }

            if (targetNode.NodeType == JourneyNodeType.Battle || targetNode.NodeType == JourneyNodeType.Boss)
            {
                if (!TryPrepareBattleEncounter(targetNode, out blockMessage))
                {
                    return false;
                }
            }
            else
            {
                _activeBattleEncounterConfig = null;
            }

            _activeJourneyNodeId = targetNode.Id;
            _activeJourneyNodeType = targetNode.NodeType;
            _activeJourneySceneName = ResolveJourneySceneName(targetNode.NodeType);
            ClearJourneyAdvanceBlockMessage();

            Publish(new JourneyNodeEnteredEvent(
                currentNode.Id,
                targetNode.Id,
                targetNode.NodeType,
                _activeJourneySceneName));
            NotifyStateChanged();

            blockMessage = string.Empty;
            return true;
        }

        public bool TryCompleteActiveJourneyNode(out string blockMessage)
        {
            if (!HasActiveJourneyEncounter)
            {
                return BlockJourneyAdvance(
                    JourneyAdvanceBlockReason.EncounterNotActive,
                    "No active node encounter to complete.",
                    out blockMessage);
            }

            int completedNodeId = _activeJourneyNodeId;
            JourneyNodeType completedNodeType = _activeJourneyNodeType;
            int foodBefore = GetResource(ResourceType.Food);
            int foodCost = FoodCostPerAdvance;

            SetJourneyProgress(
                JourneyState.Chapter,
                completedNodeId,
                JourneyState.NodesVisited + 1);

            AddResource(ResourceType.Food, -foodCost);
            int foodAfter = GetResource(ResourceType.Food);
            if (CrisisGainPerAdvance > 0)
            {
                AddResource(ResourceType.Crisis, CrisisGainPerAdvance);
            }

            ResetJourneyEncounterState();
            Publish(new JourneyNodeCompletedEvent(completedNodeId, completedNodeType, foodCost, foodBefore, foodAfter));
            NotifyStateChanged();

            if (foodAfter <= 0)
            {
                string depletionMessage = "Food is depleted. You cannot advance to the next node.";
                _lastJourneyAdvanceBlockMessage = depletionMessage;
                Publish(new JourneyAdvanceBlockedEvent(
                    JourneyAdvanceBlockReason.InsufficientFood,
                    depletionMessage,
                    JourneyState.NodeIndex,
                    foodAfter));
                NotifyStateChanged();
            }
            else
            {
                ClearJourneyAdvanceBlockMessage();
            }

            blockMessage = string.Empty;
            return true;
        }

        public bool TryDrawCard(out CardConfig card)
        {
            if (_cardPool.Count == 0)
            {
                card = null;
                return false;
            }

            card = _cardPool[0];
            _cardPool.RemoveAt(0);
            Publish(new CardDrawnEvent(card, _cardPool.Count));
            NotifyStateChanged();
            return true;
        }

        public void SetCardPool(IEnumerable<CardConfig> cards)
        {
            _cardPool.Clear();
            AddUniqueConfigs(_cardPool, cards);
            NotifyStateChanged();
        }

        public void SetEventPool(IEnumerable<EventConfig> events)
        {
            _eventPool.Clear();
            AddUniqueConfigs(_eventPool, events);
            ResetDisasterState();
            NotifyStateChanged();
        }

        public void SetEnemyPool(IEnumerable<EnemyConfig> enemies)
        {
            _enemyPool.Clear();
            AddUniqueConfigs(_enemyPool, enemies);
            BuildBattleNodeEncounterConfigs();
            NotifyStateChanged();
        }

        public void RegenerateJourneyMap()
        {
            RegenerateJourneyMap(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        }

        public void RegenerateJourneyMap(int seed)
        {
            BuildJourneyMap(seed);
            JourneyState.RunSeed = seed;
            JourneyState.NodeIndex = 0;
            ResetJourneyEncounterState();
            ClearJourneyAdvanceBlockMessage();
            ResetDisasterState();
            Publish(new JourneyMapGeneratedEvent(_journeyMap));
            NotifyStateChanged();
        }

        private void BuildResources()
        {
            _resources.Clear();
            Array resourceTypes = Enum.GetValues(typeof(ResourceType));
            for (int i = 0; i < resourceTypes.Length; i++)
            {
                ResourceType type = (ResourceType)resourceTypes.GetValue(i);
                _resources[type] = 0;
            }

            if (_resourceTable == null)
            {
                return;
            }

            IReadOnlyList<ResourceAmount> startingResources = _resourceTable.StartingResources;
            for (int i = 0; i < startingResources.Count; i++)
            {
                ResourceAmount value = startingResources[i];
                _resources[value.Type] = GetResource(value.Type) + value.Amount;
            }

            _resources[ResourceType.Crisis] = _resourceTable.StartingCrisis;
        }

        private void BuildPools()
        {
            _cardPool.Clear();
            _eventPool.Clear();
            _enemyPool.Clear();

            AddUniqueConfigs(_cardPool, _startingCardPool);
            AddUniqueConfigs(_eventPool, _startingEventPool);
            AddUniqueConfigs(_enemyPool, _startingEnemyPool);
        }

        private static void AddUniqueConfigs<T>(List<T> target, IEnumerable<T> source) where T : class
        {
            if (source == null)
            {
                return;
            }

            foreach (T asset in source)
            {
                if (asset == null || target.Contains(asset))
                {
                    continue;
                }

                target.Add(asset);
            }
        }

        private void NotifyStateChanged()
        {
            if (!_isInitialized)
            {
                return;
            }

            StateChanged?.Invoke();
        }

        private void BuildJourneyMap(int seed)
        {
            _journeyMap = JourneyMapGenerator.Generate(seed, _journeyMapGenerationConfig);
            BuildBattleNodeEncounterConfigs();
        }

        private string ResolveJourneySceneName(JourneyNodeType nodeType)
        {
            return nodeType switch
            {
                JourneyNodeType.Battle => _battleSceneName,
                JourneyNodeType.Event => _eventSceneName,
                JourneyNodeType.Supply => _supplySceneName,
                JourneyNodeType.Boss => _bossSceneName,
                _ => string.Empty
            };
        }

        private bool TryPrepareBattleEncounter(JourneyMapNode targetNode, out string blockMessage)
        {
            if (!_battleNodeEncounterConfigs.TryGetValue(targetNode.Id, out BattleEncounterConfig encounterConfig))
            {
                return BlockJourneyAdvance(
                    JourneyAdvanceBlockReason.MissingBattleEncounterConfig,
                    $"Battle encounter config is missing for node {targetNode.Id}.",
                    out blockMessage);
            }

            _activeBattleEncounterConfig = encounterConfig;
            Publish(new BattleEncounterPreparedEvent(
                encounterConfig.NodeId,
                encounterConfig.NodeType,
                encounterConfig.EncounterSeed,
                encounterConfig.EnemyQueue));
            blockMessage = string.Empty;
            return true;
        }

        private void BuildBattleNodeEncounterConfigs()
        {
            _battleNodeEncounterConfigs.Clear();

            if (_journeyMap == null || _enemyPool.Count == 0)
            {
                return;
            }

            int runSeed = _journeyMap.Seed;
            for (int i = 0; i < _journeyMap.Nodes.Count; i++)
            {
                JourneyMapNode node = _journeyMap.Nodes[i];
                if (node.NodeType != JourneyNodeType.Battle && node.NodeType != JourneyNodeType.Boss)
                {
                    continue;
                }

                int encounterSeed = ComputeBattleEncounterSeed(runSeed, node.Id, node.NodeType);
                var random = new System.Random(encounterSeed);
                int enemyCount = RollBattleEnemyCount(random, node.NodeType);
                var queue = new List<EnemyConfig>(enemyCount);
                for (int index = 0; index < enemyCount; index++)
                {
                    queue.Add(_enemyPool[random.Next(0, _enemyPool.Count)]);
                }

                _battleNodeEncounterConfigs[node.Id] = new BattleEncounterConfig(
                    node.Id,
                    node.NodeType,
                    encounterSeed,
                    queue);
            }
        }

        private int RollBattleEnemyCount(System.Random random, JourneyNodeType nodeType)
        {
            int minCount = nodeType == JourneyNodeType.Boss ? Mathf.Max(1, _bossEnemyCountMin) : Mathf.Max(1, _battleEnemyCountMin);
            int maxCount = nodeType == JourneyNodeType.Boss ? Mathf.Max(minCount, _bossEnemyCountMax) : Mathf.Max(minCount, _battleEnemyCountMax);
            return random.Next(minCount, maxCount + 1);
        }

        private static int ComputeBattleEncounterSeed(int runSeed, int nodeId, JourneyNodeType nodeType)
        {
            unchecked
            {
                int hash = runSeed;
                hash = (hash * 397) ^ nodeId;
                hash = (hash * 397) ^ (int)nodeType;
                return hash;
            }
        }

        private bool BlockJourneyAdvance(JourneyAdvanceBlockReason reason, string message, out string blockMessage)
        {
            _lastJourneyAdvanceBlockMessage = message ?? string.Empty;
            Publish(new JourneyAdvanceBlockedEvent(
                reason,
                _lastJourneyAdvanceBlockMessage,
                JourneyState.NodeIndex,
                GetResource(ResourceType.Food)));
            NotifyStateChanged();
            blockMessage = _lastJourneyAdvanceBlockMessage;
            return false;
        }

        private void ResetJourneyEncounterState()
        {
            _activeJourneyNodeId = -1;
            _activeJourneyNodeType = JourneyNodeType.Battle;
            _activeJourneySceneName = string.Empty;
            _activeBattleEncounterConfig = null;
        }

        private void ClearJourneyAdvanceBlockMessage()
        {
            _lastJourneyAdvanceBlockMessage = string.Empty;
        }

        private void EvaluateDisasterTrigger(int previousCrisis, int currentCrisis)
        {
            if (currentCrisis < previousCrisis)
            {
                _nextDisasterTriggerThreshold = CalculateNextDisasterThreshold(currentCrisis);
                return;
            }

            if (currentCrisis <= previousCrisis)
            {
                return;
            }

            if (_nextDisasterTriggerThreshold <= 0)
            {
                _nextDisasterTriggerThreshold = CalculateNextDisasterThreshold(previousCrisis);
            }

            int step = DisasterTriggerStep;
            while (currentCrisis >= _nextDisasterTriggerThreshold)
            {
                TriggerDisasterEvent(_nextDisasterTriggerThreshold, currentCrisis);
                if (_nextDisasterTriggerThreshold > int.MaxValue - step)
                {
                    _nextDisasterTriggerThreshold = int.MaxValue;
                    break;
                }

                _nextDisasterTriggerThreshold += step;
            }
        }

        private void TriggerDisasterEvent(int triggerThreshold, int currentCrisis)
        {
            bool usedFallbackEvent;
            if (!TryPickDisasterEvent(out EventConfig selectedEvent, out usedFallbackEvent))
            {
                _pendingDisasterEvent = null;
                _pendingDisasterType = DisasterEventType.None;
                _lastDisasterTriggerMessage = "Crisis threshold reached, but no event is available in the event pool.";
                Publish(new CrisisDisasterTriggeredEvent(
                    currentCrisis,
                    triggerThreshold,
                    null,
                    DisasterEventType.None,
                    false));
                return;
            }

            _pendingDisasterEvent = selectedEvent;
            _pendingDisasterType = selectedEvent.DisasterType;
            _lastDisasterTriggerMessage = usedFallbackEvent
                ? $"Crisis reached {triggerThreshold}. Fallback event '{selectedEvent.DisplayName}' triggered as disaster."
                : $"Crisis reached {triggerThreshold}. Disaster '{selectedEvent.DisplayName}' triggered ({selectedEvent.DisasterType}).";

            Publish(new CrisisDisasterTriggeredEvent(
                currentCrisis,
                triggerThreshold,
                selectedEvent,
                selectedEvent.DisasterType,
                usedFallbackEvent));
        }

        private bool TryPickDisasterEvent(out EventConfig selectedEvent, out bool usedFallbackEvent)
        {
            int disasterCount = 0;
            for (int i = 0; i < _eventPool.Count; i++)
            {
                EventConfig gameEvent = _eventPool[i];
                if (gameEvent != null && gameEvent.IsDisasterEvent)
                {
                    disasterCount++;
                }
            }

            if (disasterCount > 0)
            {
                int pickIndex = UnityEngine.Random.Range(0, disasterCount);
                for (int i = 0; i < _eventPool.Count; i++)
                {
                    EventConfig gameEvent = _eventPool[i];
                    if (gameEvent == null || !gameEvent.IsDisasterEvent)
                    {
                        continue;
                    }

                    if (pickIndex == 0)
                    {
                        selectedEvent = gameEvent;
                        usedFallbackEvent = false;
                        return true;
                    }

                    pickIndex--;
                }
            }

            int eventCount = 0;
            for (int i = 0; i < _eventPool.Count; i++)
            {
                if (_eventPool[i] != null)
                {
                    eventCount++;
                }
            }

            if (eventCount == 0)
            {
                selectedEvent = null;
                usedFallbackEvent = false;
                return false;
            }

            int fallbackPick = UnityEngine.Random.Range(0, eventCount);
            for (int i = 0; i < _eventPool.Count; i++)
            {
                EventConfig gameEvent = _eventPool[i];
                if (gameEvent == null)
                {
                    continue;
                }

                if (fallbackPick == 0)
                {
                    selectedEvent = gameEvent;
                    usedFallbackEvent = true;
                    return true;
                }

                fallbackPick--;
            }

            selectedEvent = null;
            usedFallbackEvent = false;
            return false;
        }

        private void ResetDisasterState()
        {
            _pendingDisasterEvent = null;
            _pendingDisasterType = DisasterEventType.None;
            _lastDisasterTriggerMessage = string.Empty;
            _nextDisasterTriggerThreshold = CalculateNextDisasterThreshold(GetResource(ResourceType.Crisis));
        }

        private int CalculateNextDisasterThreshold(int currentCrisis)
        {
            int baseThreshold = DisasterTriggerThreshold;
            if (currentCrisis < baseThreshold)
            {
                return baseThreshold;
            }

            int step = DisasterTriggerStep;
            long increments = ((long)currentCrisis - baseThreshold) / step + 1;
            long nextThreshold = (long)baseThreshold + increments * step;
            return nextThreshold > int.MaxValue ? int.MaxValue : (int)nextThreshold;
        }

        private void Publish<TEvent>(TEvent gameEvent)
        {
            _eventBus?.Publish(gameEvent);
        }

        private void LoadDefaultsIfNeeded()
        {
            _startingCardPool.RemoveAll(card => card == null);
            _startingEventPool.RemoveAll(gameEvent => gameEvent == null);
            _startingEnemyPool.RemoveAll(enemy => enemy == null);

#if UNITY_EDITOR
            if (!_autoLoadEditorData)
            {
                return;
            }

            if (_resourceTable == null)
            {
                _resourceTable = LoadFirstAsset<ResourceTableConfig>();
            }

            if (_startingCardPool.Count == 0)
            {
                _startingCardPool = LoadAssets<CardConfig>();
            }

            if (_startingEventPool.Count == 0)
            {
                _startingEventPool = LoadAssets<EventConfig>();
            }

            if (_startingEnemyPool.Count == 0)
            {
                _startingEnemyPool = LoadAssets<EnemyConfig>();
            }
#endif
        }

#if UNITY_EDITOR
        private static T LoadFirstAsset<T>() where T : ScriptableObject
        {
            List<T> assets = LoadAssets<T>();
            return assets.Count > 0 ? assets[0] : null;
        }

        private static List<T> LoadAssets<T>() where T : ScriptableObject
        {
            string[] folder = { "Assets/Data" };
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", folder);
            if (guids.Length == 0)
            {
                string[] allAssets = { "Assets" };
                guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", allAssets);
            }

            List<T> results = new List<T>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null || results.Contains(asset))
                {
                    continue;
                }

                results.Add(asset);
            }

            return results;
        }
#endif
    }
}
