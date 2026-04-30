using System;
using System.Collections.Generic;
using System.Text;
using OneManJourney.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameContextDebugPanel : MonoBehaviour
    {
        private readonly StringBuilder _builder = new StringBuilder(512);
        private GameContext _context;
        private BattleTurnController _battleTurnController;
        private GameEventBus _eventBus;
        private IDisposable _resourceChangedSubscription;
        private IDisposable _nodeSelectedSubscription;
        private IDisposable _cardDrawnSubscription;
        private IDisposable _contextInitializedSubscription;
        private IDisposable _journeyMapGeneratedSubscription;
        private IDisposable _journeyNodeEnteredSubscription;
        private IDisposable _journeyNodeCompletedSubscription;
        private IDisposable _journeyAdvanceBlockedSubscription;
        private IDisposable _crisisDisasterTriggeredSubscription;
        private IDisposable _battleEncounterPreparedSubscription;
        private IDisposable _battleFlowInitializedSubscription;
        private IDisposable _battleTurnStartedSubscription;
        private IDisposable _battleCardPlayedSubscription;
        private IDisposable _battleHandDiscardedSubscription;
        private IDisposable _battleEnemyTurnResolvedSubscription;
        private IDisposable _battleCardsDrawnSubscription;
        private IDisposable _battleFlowEndedSubscription;
        private TextMeshProUGUI _text;

        private void Awake()
        {
            EnsureUi();
            TryBindContext();
            Refresh();
        }

        private void OnEnable()
        {
            TryBindContext();
            Refresh();
        }

        private void OnDisable()
        {
            UnbindContext();
        }

        private void Update()
        {
            bool didBind = false;
            if (_context == null && TryBindContext())
            {
                didBind = true;
            }

            if (_battleTurnController == null && TryBindBattleTurnController())
            {
                didBind = true;
            }

            if (_eventBus == null && TryBindEventBus())
            {
                didBind = true;
            }

            if (didBind)
            {
                Refresh();
            }
        }

        private bool TryBindContext()
        {
            GameContext nextContext = GameContext.Instance;
            if (nextContext == null && GameServices.TryResolve(out GameContext resolved))
            {
                nextContext = resolved;
            }

            if (nextContext == null)
            {
                GameContext[] contexts = Resources.FindObjectsOfTypeAll<GameContext>();
                if (contexts != null && contexts.Length > 0)
                {
                    nextContext = contexts[0];
                }
            }

            if (_context == nextContext)
            {
                return _context != null;
            }

            UnbindContext();
            _context = nextContext;
            if (_context == null)
            {
                return false;
            }

            _context.Initialized += HandleContextStateChanged;
            _context.StateChanged += HandleContextStateChanged;
            TryBindBattleTurnController();
            TryBindEventBus();
            return true;
        }

        private void UnbindContext()
        {
            if (_context != null)
            {
                _context.Initialized -= HandleContextStateChanged;
                _context.StateChanged -= HandleContextStateChanged;
                _context = null;
            }

            _battleTurnController = null;

            UnbindEventBus();
        }

        private void HandleContextStateChanged()
        {
            Refresh();
        }

        private bool TryBindBattleTurnController()
        {
            BattleTurnController nextController = null;
            if (_context != null)
            {
                nextController = _context.GetComponent<BattleTurnController>();
            }

            if (nextController == null && GameServices.TryResolve(out BattleTurnController resolved))
            {
                nextController = resolved;
            }

            if (nextController == null)
            {
                BattleTurnController[] controllers = Resources.FindObjectsOfTypeAll<BattleTurnController>();
                if (controllers != null && controllers.Length > 0)
                {
                    nextController = controllers[0];
                }
            }

            _battleTurnController = nextController;
            return _battleTurnController != null;
        }

        private bool TryBindEventBus()
        {
            GameEventBus nextEventBus = null;
            if (_context != null)
            {
                nextEventBus = _context.EventBus;
            }

            if (nextEventBus == null && GameServices.TryResolve(out GameEventBus resolved))
            {
                nextEventBus = resolved;
            }

            if (_eventBus == nextEventBus)
            {
                return _eventBus != null;
            }

            UnbindEventBus();
            _eventBus = nextEventBus;
            if (_eventBus == null)
            {
                return false;
            }

            _resourceChangedSubscription = _eventBus.Subscribe<ResourceChangedEvent>(HandleResourceChanged);
            _nodeSelectedSubscription = _eventBus.Subscribe<NodeSelectedEvent>(HandleNodeSelected);
            _cardDrawnSubscription = _eventBus.Subscribe<CardDrawnEvent>(HandleCardDrawn);
            _contextInitializedSubscription = _eventBus.Subscribe<GameContextInitializedEvent>(HandleContextInitialized);
            _journeyMapGeneratedSubscription = _eventBus.Subscribe<JourneyMapGeneratedEvent>(HandleJourneyMapGenerated);
            _journeyNodeEnteredSubscription = _eventBus.Subscribe<JourneyNodeEnteredEvent>(HandleJourneyNodeEntered);
            _journeyNodeCompletedSubscription = _eventBus.Subscribe<JourneyNodeCompletedEvent>(HandleJourneyNodeCompleted);
            _journeyAdvanceBlockedSubscription = _eventBus.Subscribe<JourneyAdvanceBlockedEvent>(HandleJourneyAdvanceBlocked);
            _crisisDisasterTriggeredSubscription = _eventBus.Subscribe<CrisisDisasterTriggeredEvent>(HandleCrisisDisasterTriggered);
            _battleEncounterPreparedSubscription = _eventBus.Subscribe<BattleEncounterPreparedEvent>(HandleBattleEncounterPrepared);
            _battleFlowInitializedSubscription = _eventBus.Subscribe<BattleFlowInitializedEvent>(HandleBattleFlowInitialized);
            _battleTurnStartedSubscription = _eventBus.Subscribe<BattleTurnStartedEvent>(HandleBattleTurnStarted);
            _battleCardPlayedSubscription = _eventBus.Subscribe<BattleCardPlayedEvent>(HandleBattleCardPlayed);
            _battleHandDiscardedSubscription = _eventBus.Subscribe<BattleHandDiscardedEvent>(HandleBattleHandDiscarded);
            _battleEnemyTurnResolvedSubscription = _eventBus.Subscribe<BattleEnemyTurnResolvedEvent>(HandleBattleEnemyTurnResolved);
            _battleCardsDrawnSubscription = _eventBus.Subscribe<BattleCardsDrawnEvent>(HandleBattleCardsDrawn);
            _battleFlowEndedSubscription = _eventBus.Subscribe<BattleFlowEndedEvent>(HandleBattleFlowEnded);
            return true;
        }

        private void UnbindEventBus()
        {
            _resourceChangedSubscription?.Dispose();
            _nodeSelectedSubscription?.Dispose();
            _cardDrawnSubscription?.Dispose();
            _contextInitializedSubscription?.Dispose();
            _journeyMapGeneratedSubscription?.Dispose();
            _journeyNodeEnteredSubscription?.Dispose();
            _journeyNodeCompletedSubscription?.Dispose();
            _journeyAdvanceBlockedSubscription?.Dispose();
            _crisisDisasterTriggeredSubscription?.Dispose();
            _battleEncounterPreparedSubscription?.Dispose();
            _battleFlowInitializedSubscription?.Dispose();
            _battleTurnStartedSubscription?.Dispose();
            _battleCardPlayedSubscription?.Dispose();
            _battleHandDiscardedSubscription?.Dispose();
            _battleEnemyTurnResolvedSubscription?.Dispose();
            _battleCardsDrawnSubscription?.Dispose();
            _battleFlowEndedSubscription?.Dispose();

            _resourceChangedSubscription = null;
            _nodeSelectedSubscription = null;
            _cardDrawnSubscription = null;
            _contextInitializedSubscription = null;
            _journeyMapGeneratedSubscription = null;
            _journeyNodeEnteredSubscription = null;
            _journeyNodeCompletedSubscription = null;
            _journeyAdvanceBlockedSubscription = null;
            _crisisDisasterTriggeredSubscription = null;
            _battleEncounterPreparedSubscription = null;
            _battleFlowInitializedSubscription = null;
            _battleTurnStartedSubscription = null;
            _battleCardPlayedSubscription = null;
            _battleHandDiscardedSubscription = null;
            _battleEnemyTurnResolvedSubscription = null;
            _battleCardsDrawnSubscription = null;
            _battleFlowEndedSubscription = null;
            _eventBus = null;
        }

        private void HandleResourceChanged(ResourceChangedEvent _)
        {
            Refresh();
        }

        private void HandleNodeSelected(NodeSelectedEvent _)
        {
            Refresh();
        }

        private void HandleCardDrawn(CardDrawnEvent _)
        {
            Refresh();
        }

        private void HandleContextInitialized(GameContextInitializedEvent _)
        {
            Refresh();
        }

        private void HandleJourneyMapGenerated(JourneyMapGeneratedEvent _)
        {
            Refresh();
        }

        private void HandleJourneyNodeEntered(JourneyNodeEnteredEvent _)
        {
            Refresh();
        }

        private void HandleJourneyNodeCompleted(JourneyNodeCompletedEvent _)
        {
            Refresh();
        }

        private void HandleJourneyAdvanceBlocked(JourneyAdvanceBlockedEvent _)
        {
            Refresh();
        }

        private void HandleCrisisDisasterTriggered(CrisisDisasterTriggeredEvent _)
        {
            Refresh();
        }

        private void HandleBattleEncounterPrepared(BattleEncounterPreparedEvent _)
        {
            Refresh();
        }

        private void HandleBattleFlowInitialized(BattleFlowInitializedEvent _)
        {
            Refresh();
        }

        private void HandleBattleTurnStarted(BattleTurnStartedEvent _)
        {
            Refresh();
        }

        private void HandleBattleCardPlayed(BattleCardPlayedEvent _)
        {
            Refresh();
        }

        private void HandleBattleHandDiscarded(BattleHandDiscardedEvent _)
        {
            Refresh();
        }

        private void HandleBattleEnemyTurnResolved(BattleEnemyTurnResolvedEvent _)
        {
            Refresh();
        }

        private void HandleBattleCardsDrawn(BattleCardsDrawnEvent _)
        {
            Refresh();
        }

        private void HandleBattleFlowEnded(BattleFlowEndedEvent _)
        {
            Refresh();
        }

        private void EnsureUi()
        {
            if (_text != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("GameContextDebugCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(16f, -16f);
            panelRect.sizeDelta = new Vector2(460f, 760f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.88f);

            GameObject textObject = new GameObject("ContextText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panelObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 12f);
            textRect.offsetMax = new Vector2(-12f, -12f);

            _text = textObject.GetComponent<TextMeshProUGUI>();
            _text.font = ResolveDebugFont();
            _text.fontSize = 16f;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.color = Color.white;
            _text.enableWordWrapping = true;
            _text.overflowMode = TextOverflowModes.Overflow;
            _text.text = "GameContext Debug Panel";
        }

        private static TMP_FontAsset ResolveDebugFont()
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            TMP_FontAsset fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (fallback == null)
            {
                Debug.LogWarning("GameContextDebugPanel: TMP font asset not found. Import TMP Essentials and retry.");
            }

            return fallback;
        }

        private void Refresh()
        {
            if (_text == null)
            {
                return;
            }

            _builder.Clear();
            _builder.AppendLine("GameContext Debug");
            _builder.AppendLine("----------------------");

            if (_context == null)
            {
                _builder.AppendLine("Context: not found");
                _text.text = _builder.ToString();
                return;
            }

            if (_battleTurnController == null)
            {
                TryBindBattleTurnController();
            }

            JourneyState state = _context.JourneyState;
            _builder.AppendLine($"Chapter: {state.Chapter}");
            _builder.AppendLine($"Node Index: {state.NodeIndex}");
            _builder.AppendLine($"Visited Nodes: {state.NodesVisited}");
            _builder.AppendLine($"Run Seed: {state.RunSeed}");
            _builder.AppendLine($"Crisis: {state.CrisisValue}");
            _builder.AppendLine($"Card Pool: {_context.CardPool.Count}");
            _builder.AppendLine($"Event Pool: {_context.EventPool.Count}");
            _builder.AppendLine($"Enemy Pool: {_context.EnemyPool.Count}");
            AppendCrisisSummary(_context);
            AppendJourneyMapSummary(_context.JourneyMap);
            AppendJourneyProgressSummary(_context);
            AppendBattleEntrySummary(_context);
            AppendBattleTurnSummary();
            _builder.AppendLine();
            _builder.AppendLine("Resources:");

            Array resourceTypes = Enum.GetValues(typeof(ResourceType));
            for (int i = 0; i < resourceTypes.Length; i++)
            {
                ResourceType type = (ResourceType)resourceTypes.GetValue(i);
                _builder.AppendLine($"- {type}: {_context.GetResource(type)}");
            }

            if (_context.CardPool.Count > 0)
            {
                _builder.AppendLine();
                _builder.AppendLine("Cards (first 5):");
                int limit = Mathf.Min(5, _context.CardPool.Count);
                for (int i = 0; i < limit; i++)
                {
                    CardConfig card = _context.CardPool[i];
                    _builder.AppendLine($"- {card.DisplayName} ({card.Id})");
                }
            }

            if (_context.EventPool.Count > 0)
            {
                _builder.AppendLine();
                _builder.AppendLine("Events (first 5):");
                int limit = Mathf.Min(5, _context.EventPool.Count);
                for (int i = 0; i < limit; i++)
                {
                    EventConfig gameEvent = _context.EventPool[i];
                    _builder.AppendLine($"- {gameEvent.DisplayName} ({gameEvent.Id})");
                }
            }

            _text.text = _builder.ToString();
        }

        private void AppendJourneyProgressSummary(GameContext context)
        {
            _builder.AppendLine();
            _builder.AppendLine("Journey Progress:");
            _builder.AppendLine($"- Food Cost / Move: {context.FoodCostPerAdvance}");
            if (context.HasActiveJourneyEncounter)
            {
                _builder.AppendLine($"- Active Encounter: Node {context.ActiveJourneyNodeId} ({context.ActiveJourneyNodeType})");
                if (!string.IsNullOrWhiteSpace(context.ActiveJourneySceneName))
                {
                    _builder.AppendLine($"- Scene: {context.ActiveJourneySceneName}");
                }
            }
            else
            {
                _builder.AppendLine("- Active Encounter: None");
            }

            if (!string.IsNullOrWhiteSpace(context.LastJourneyAdvanceBlockMessage))
            {
                _builder.AppendLine($"- Blocked: {context.LastJourneyAdvanceBlockMessage}");
            }

            IReadOnlyList<JourneyMapNode> nextNodes = context.GetAvailableNextJourneyNodes();
            if (nextNodes.Count == 0)
            {
                _builder.AppendLine("- Next Nodes: None");
                return;
            }

            _builder.AppendLine("- Next Nodes:");
            for (int i = 0; i < nextNodes.Count; i++)
            {
                JourneyMapNode node = nextNodes[i];
                _builder.AppendLine($"  - #{node.Id} ({node.NodeType})");
            }
        }

        private void AppendBattleTurnSummary()
        {
            _builder.AppendLine();
            _builder.AppendLine("Battle Turn:");

            if (_battleTurnController == null)
            {
                _builder.AppendLine("- Controller: not found");
                return;
            }

            if (!_battleTurnController.IsActive)
            {
                _builder.AppendLine("- Active: No");
                return;
            }

            _builder.AppendLine($"- Active: Yes (Node {_battleTurnController.ActiveNodeId}, {_battleTurnController.ActiveNodeType})");
            _builder.AppendLine($"- Phase: {_battleTurnController.Phase}");
            _builder.AppendLine($"- Turn: {_battleTurnController.TurnNumber}");
            _builder.AppendLine($"- Energy: {_battleTurnController.CurrentEnergy}/{_battleTurnController.MaxEnergyPerTurn}");
            _builder.AppendLine($"- Enemy Count: {_battleTurnController.EnemyQueue.Count}");
            _builder.AppendLine($"- Draw/Hand/Discard/Exhaust: {_battleTurnController.DrawPile.Count}/{_battleTurnController.Hand.Count}/{_battleTurnController.DiscardPile.Count}/{_battleTurnController.ExhaustPile.Count}");

            if (_battleTurnController.Hand.Count == 0)
            {
                _builder.AppendLine("- Hand: empty");
                return;
            }

            _builder.AppendLine("- Hand (first 5):");
            int limit = Mathf.Min(5, _battleTurnController.Hand.Count);
            for (int index = 0; index < limit; index++)
            {
                CardConfig card = _battleTurnController.Hand[index];
                if (card == null)
                {
                    _builder.AppendLine("  - null");
                    continue;
                }

                _builder.AppendLine($"  - {card.DisplayName} ({card.EnergyCost})");
            }
        }

        private void AppendCrisisSummary(GameContext context)
        {
            _builder.AppendLine();
            _builder.AppendLine("Crisis System:");
            _builder.AppendLine($"- Gain / Advance: {context.CrisisGainPerAdvance}");
            _builder.AppendLine($"- Trigger Threshold: {context.DisasterTriggerThreshold}");
            _builder.AppendLine($"- Trigger Step: {context.DisasterTriggerStep}");
            _builder.AppendLine($"- Next Trigger At: {context.NextDisasterTriggerThreshold}");

            if (context.PendingDisasterEvent == null)
            {
                _builder.AppendLine("- Pending Disaster: None");
            }
            else
            {
                _builder.AppendLine($"- Pending Disaster: {context.PendingDisasterEvent.DisplayName} ({context.PendingDisasterType})");
            }

            if (!string.IsNullOrWhiteSpace(context.LastDisasterTriggerMessage))
            {
                _builder.AppendLine($"- Last Trigger: {context.LastDisasterTriggerMessage}");
            }
        }

        private void AppendBattleEntrySummary(GameContext context)
        {
            _builder.AppendLine();
            _builder.AppendLine("Battle Entry:");

            if (!context.HasActiveJourneyEncounter)
            {
                _builder.AppendLine("- Active Encounter: None");
                return;
            }

            if (context.ActiveJourneyNodeType != JourneyNodeType.Battle
                && context.ActiveJourneyNodeType != JourneyNodeType.Boss)
            {
                _builder.AppendLine("- Active Encounter: Non-battle node");
                return;
            }

            _builder.AppendLine($"- Active Node: {context.ActiveJourneyNodeId} ({context.ActiveJourneyNodeType})");

            if (context.TryGetBattleNodeEncounterConfig(context.ActiveJourneyNodeId, out BattleEncounterConfig nodeConfig))
            {
                _builder.AppendLine($"- Node Config Seed: {nodeConfig.EncounterSeed}");
                _builder.AppendLine($"- Node Config Queue: {FormatEnemyQueue(nodeConfig.EnemyQueue)}");
            }
            else
            {
                _builder.AppendLine("- Node Config: Missing");
            }

            if (context.ActiveBattleEncounterConfig == null)
            {
                _builder.AppendLine("- Active Queue: Missing");
                return;
            }

            BattleEncounterConfig activeConfig = context.ActiveBattleEncounterConfig;
            _builder.AppendLine($"- Active Queue Seed: {activeConfig.EncounterSeed}");
            _builder.AppendLine($"- Active Queue: {FormatEnemyQueue(activeConfig.EnemyQueue)}");
            bool matches = context.TryGetBattleNodeEncounterConfig(activeConfig.NodeId, out BattleEncounterConfig expected)
                && HasSameEnemyQueue(expected.EnemyQueue, activeConfig.EnemyQueue);
            _builder.AppendLine($"- Queue Matches Node Config: {matches}");
        }

        private static string FormatEnemyQueue(IReadOnlyList<EnemyConfig> queue)
        {
            if (queue == null || queue.Count == 0)
            {
                return "none";
            }

            var builder = new StringBuilder();
            for (int index = 0; index < queue.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                EnemyConfig enemy = queue[index];
                if (enemy == null)
                {
                    builder.Append("null");
                    continue;
                }

                builder.Append(enemy.DisplayName);
                builder.Append(" (");
                builder.Append(enemy.Id);
                builder.Append(')');
            }

            return builder.ToString();
        }

        private static bool HasSameEnemyQueue(IReadOnlyList<EnemyConfig> left, IReadOnlyList<EnemyConfig> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (int index = 0; index < left.Count; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }

        private void AppendJourneyMapSummary(JourneyMap journeyMap)
        {
            _builder.AppendLine();
            _builder.AppendLine("Journey Map:");
            if (journeyMap == null)
            {
                _builder.AppendLine("- Not Generated");
                return;
            }

            _builder.AppendLine($"- Nodes: {journeyMap.Nodes.Count}");
            _builder.AppendLine($"- Routes: {journeyMap.RouteCount}");
            _builder.AppendLine($"- Branching Nodes: {journeyMap.BranchingNodeCount}");
            _builder.AppendLine($"- Battle/Event/Supply/Boss: {journeyMap.GetTypeCount(JourneyNodeType.Battle)}/{journeyMap.GetTypeCount(JourneyNodeType.Event)}/{journeyMap.GetTypeCount(JourneyNodeType.Supply)}/{journeyMap.GetTypeCount(JourneyNodeType.Boss)}");
        }
    }
}
