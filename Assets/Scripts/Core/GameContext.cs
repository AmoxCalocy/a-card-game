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
        [SerializeField] private bool _autoLoadEditorData = true;

        private readonly Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();
        private readonly List<CardConfig> _cardPool = new List<CardConfig>();
        private readonly List<EventConfig> _eventPool = new List<EventConfig>();
        private GameEventBus _eventBus;
        private bool _isInitialized;

        public static GameContext Instance { get; private set; }

        public event Action Initialized;
        public event Action StateChanged;

        public ResourceTableConfig ResourceTable => _resourceTable;
        public IReadOnlyDictionary<ResourceType, int> Resources => _resources;
        public IReadOnlyList<CardConfig> CardPool => _cardPool;
        public IReadOnlyList<EventConfig> EventPool => _eventPool;
        public GameEventBus EventBus => _eventBus;
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

            _isInitialized = true;
            Initialized?.Invoke();
            Publish(new GameContextInitializedEvent(
                JourneyState.Chapter,
                JourneyState.NodeIndex,
                JourneyState.NodesVisited,
                JourneyState.CrisisValue,
                _cardPool.Count,
                _eventPool.Count));
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

            if (type == ResourceType.Crisis)
            {
                JourneyState.CrisisValue = amount;
            }

            if (previousAmount == amount)
            {
                return;
            }

            _resources[type] = amount;

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
            SetJourneyProgress(JourneyState.Chapter, JourneyState.NodeIndex + 1, JourneyState.NodesVisited + 1);
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

            AddUniqueConfigs(_cardPool, _startingCardPool);
            AddUniqueConfigs(_eventPool, _startingEventPool);
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

        private void Publish<TEvent>(TEvent gameEvent)
        {
            _eventBus?.Publish(gameEvent);
        }

        private void LoadDefaultsIfNeeded()
        {
            _startingCardPool.RemoveAll(card => card == null);
            _startingEventPool.RemoveAll(gameEvent => gameEvent == null);

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
