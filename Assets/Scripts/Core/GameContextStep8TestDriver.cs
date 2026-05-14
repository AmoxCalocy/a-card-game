using System;
using System.Collections.Generic;
using OneManJourney.Data;
using UnityEngine;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameContextStep8TestDriver : MonoBehaviour
    {
        private static GameContextStep8TestDriver _instance;

        [Header("Hotkeys")]
        [SerializeField] private KeyCode _selectFirstNodeHotkey = KeyCode.Alpha1;
        [SerializeField] private KeyCode _selectSecondNodeHotkey = KeyCode.Alpha2;
        [SerializeField] private KeyCode _completeNodeHotkey = KeyCode.Return;
        [SerializeField] private KeyCode _depleteFoodHotkey = KeyCode.F;
        [SerializeField] private KeyCode _playCardHotkey = KeyCode.P;
        [SerializeField] private KeyCode _endTurnHotkey = KeyCode.E;

        private GameContext _context;
        private BattleTurnController _battleTurnController;
        private GameEventBus _eventBus;
        private IDisposable _journeyNodeEnteredSubscription;
        private IDisposable _journeyNodeCompletedSubscription;
        private IDisposable _journeyAdvanceBlockedSubscription;
        private IDisposable _battleEncounterPreparedSubscription;
        private IDisposable _battleFlowInitializedSubscription;
        private IDisposable _battleEnemyIntentUpdatedSubscription;
        private IDisposable _battleTurnStartedSubscription;
        private IDisposable _battleCardPlayedSubscription;
        private IDisposable _battleHandDiscardedSubscription;
        private IDisposable _battleEnemyTurnResolvedSubscription;
        private IDisposable _battleCardsDrawnSubscription;
        private IDisposable _battleSettledSubscription;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            TryBind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Update()
        {
            if (!TryBind())
            {
                return;
            }

            if (Input.GetKeyDown(_depleteFoodHotkey))
            {
                _context.SetResource(ResourceType.Food, 0);
            }

            if (_battleTurnController != null && _battleTurnController.IsActive)
            {
                if (Input.GetKeyDown(_playCardHotkey) && !_battleTurnController.TryPlayFirstCard(out string playMessage))
                {
                    Debug.LogWarning($"Step11TestDriver Action: play failed. {playMessage}");
                }

                if (Input.GetKeyDown(_endTurnHotkey) && !_battleTurnController.TryEndPlayerTurn(out string endTurnMessage))
                {
                    Debug.LogWarning($"Step11TestDriver Action: end turn failed. {endTurnMessage}");
                }
            }

            if (_context.HasActiveJourneyEncounter)
            {
                if (Input.GetKeyDown(_completeNodeHotkey))
                {
                    _context.TryCompleteActiveJourneyNode(out _);
                }

                return;
            }

            IReadOnlyList<JourneyMapNode> nextNodes = _context.GetAvailableNextJourneyNodes();
            if (nextNodes.Count > 0 && Input.GetKeyDown(_selectFirstNodeHotkey))
            {
                _context.TryEnterNextJourneyNode(nextNodes[0].Id, out _);
            }

            if (nextNodes.Count > 1 && Input.GetKeyDown(_selectSecondNodeHotkey))
            {
                _context.TryEnterNextJourneyNode(nextNodes[1].Id, out _);
            }
        }

        private bool TryBind()
        {
            if (_context == null)
            {
                _context = GameContext.Instance;
                if (_context == null)
                {
                    GameServices.TryResolve(out _context);
                }
            }

            if (_eventBus == null)
            {
                if (_context != null)
                {
                    _eventBus = _context.EventBus;
                }

                if (_eventBus == null)
                {
                    GameServices.TryResolve(out _eventBus);
                }
            }

            if (_battleTurnController == null)
            {
                if (_context != null)
                {
                    _battleTurnController = _context.GetComponent<BattleTurnController>();
                }

                if (_battleTurnController == null)
                {
                    GameServices.TryResolve(out _battleTurnController);
                }
            }

            if (_context == null || _eventBus == null)
            {
                return false;
            }

            if (_journeyNodeEnteredSubscription == null)
            {
                _journeyNodeEnteredSubscription = _eventBus.Subscribe<JourneyNodeEnteredEvent>(HandleJourneyNodeEntered);
                _journeyNodeCompletedSubscription = _eventBus.Subscribe<JourneyNodeCompletedEvent>(HandleJourneyNodeCompleted);
                _journeyAdvanceBlockedSubscription = _eventBus.Subscribe<JourneyAdvanceBlockedEvent>(HandleJourneyAdvanceBlocked);
                _battleEncounterPreparedSubscription = _eventBus.Subscribe<BattleEncounterPreparedEvent>(HandleBattleEncounterPrepared);
                _battleFlowInitializedSubscription = _eventBus.Subscribe<BattleFlowInitializedEvent>(HandleBattleFlowInitialized);
                _battleEnemyIntentUpdatedSubscription = _eventBus.Subscribe<BattleEnemyIntentUpdatedEvent>(HandleBattleEnemyIntentUpdated);
                _battleTurnStartedSubscription = _eventBus.Subscribe<BattleTurnStartedEvent>(HandleBattleTurnStarted);
                _battleCardPlayedSubscription = _eventBus.Subscribe<BattleCardPlayedEvent>(HandleBattleCardPlayed);
                _battleHandDiscardedSubscription = _eventBus.Subscribe<BattleHandDiscardedEvent>(HandleBattleHandDiscarded);
                _battleEnemyTurnResolvedSubscription = _eventBus.Subscribe<BattleEnemyTurnResolvedEvent>(HandleBattleEnemyTurnResolved);
                _battleCardsDrawnSubscription = _eventBus.Subscribe<BattleCardsDrawnEvent>(HandleBattleCardsDrawn);
                _battleSettledSubscription = _eventBus.Subscribe<BattleSettledEvent>(HandleBattleSettled);
            }

            return true;
        }

        private void Unbind()
        {
            _journeyNodeEnteredSubscription?.Dispose();
            _journeyNodeCompletedSubscription?.Dispose();
            _journeyAdvanceBlockedSubscription?.Dispose();
            _battleEncounterPreparedSubscription?.Dispose();
            _battleFlowInitializedSubscription?.Dispose();
            _battleEnemyIntentUpdatedSubscription?.Dispose();
            _battleTurnStartedSubscription?.Dispose();
            _battleCardPlayedSubscription?.Dispose();
            _battleHandDiscardedSubscription?.Dispose();
            _battleEnemyTurnResolvedSubscription?.Dispose();
            _battleCardsDrawnSubscription?.Dispose();
            _battleSettledSubscription?.Dispose();
            _journeyNodeEnteredSubscription = null;
            _journeyNodeCompletedSubscription = null;
            _journeyAdvanceBlockedSubscription = null;
            _battleEncounterPreparedSubscription = null;
            _battleFlowInitializedSubscription = null;
            _battleEnemyIntentUpdatedSubscription = null;
            _battleTurnStartedSubscription = null;
            _battleCardPlayedSubscription = null;
            _battleHandDiscardedSubscription = null;
            _battleEnemyTurnResolvedSubscription = null;
            _battleCardsDrawnSubscription = null;
            _battleSettledSubscription = null;
            _eventBus = null;
            _context = null;
            _battleTurnController = null;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private static void HandleJourneyNodeEntered(JourneyNodeEnteredEvent evt)
        {
            Debug.Log(
                "Step8TestDriver Event: NodeEntered " +
                $"from={evt.PreviousNodeId}, to={evt.TargetNodeId}, type={evt.NodeType}, scene='{evt.SceneName}'.");
        }

        private static void HandleJourneyNodeCompleted(JourneyNodeCompletedEvent evt)
        {
            Debug.Log(
                "Step8TestDriver Event: NodeCompleted " +
                $"node={evt.NodeId}, type={evt.NodeType}, food={evt.FoodBefore}->{evt.FoodAfter} (cost {evt.FoodCost}).");
        }

        private static void HandleJourneyAdvanceBlocked(JourneyAdvanceBlockedEvent evt)
        {
            Debug.LogWarning(
                "Step8TestDriver Event: AdvanceBlocked " +
                $"reason={evt.Reason}, node={evt.CurrentNodeId}, food={evt.FoodAmount}, message='{evt.Message}'.");
        }

        private static void HandleBattleEncounterPrepared(BattleEncounterPreparedEvent evt)
        {
            Debug.Log(
                "Step8TestDriver Event: BattleEncounterPrepared " +
                $"node={evt.NodeId}, type={evt.NodeType}, seed={evt.EncounterSeed}, enemyCount={evt.EnemyCount}, " +
                $"queue=[{FormatEnemyQueue(evt.EnemyQueue)}].");
        }

        private static void HandleBattleFlowInitialized(BattleFlowInitializedEvent evt)
        {
            Debug.Log(
                "Step11TestDriver Event: BattleFlowInitialized " +
                $"node={evt.NodeId}, type={evt.NodeType}, seed={evt.EncounterSeed}, enemies={evt.EnemyCount}, " +
                $"draw/hand/discard/exhaust={evt.DrawPileCount}/{evt.HandCount}/{evt.DiscardPileCount}/{evt.ExhaustPileCount}.");
        }

        private static void HandleBattleTurnStarted(BattleTurnStartedEvent evt)
        {
            Debug.Log(
                "Step11TestDriver Event: TurnStarted " +
                $"node={evt.NodeId}, turn={evt.TurnNumber}, energy={evt.Energy}, drew={evt.DrawnCardCount}, " +
                $"draw/hand/discard/exhaust={evt.DrawPileCount}/{evt.HandCount}/{evt.DiscardPileCount}/{evt.ExhaustPileCount}.");
        }

        private static void HandleBattleEnemyIntentUpdated(BattleEnemyIntentUpdatedEvent evt)
        {
            Debug.Log(
                "Step13TestDriver Event: EnemyIntentUpdated " +
                $"node={evt.NodeId}, turn={evt.TurnNumber}, intents=[{FormatEnemyIntents(evt.EnemyIntents)}].");
        }

        private static void HandleBattleCardPlayed(BattleCardPlayedEvent evt)
        {
            string cardName = evt.Card == null ? "null" : $"{evt.Card.DisplayName} ({evt.Card.Id})";
            Debug.Log(
                "Step11TestDriver Event: CardPlayed " +
                $"node={evt.NodeId}, turn={evt.TurnNumber}, card={cardName}, type={evt.CardType}, value={evt.CardBaseValue}, " +
                $"status={evt.StatusEffect}x{evt.RequestedStatusStacks}, energy={evt.EnergyBefore}->{evt.EnergyAfter}, exhausted={evt.Exhausted}, " +
                $"effect(dmg/armor/heal/draw/status)={evt.DamageApplied}/{evt.ArmorApplied}/{evt.HealingApplied}/{evt.CardsDrawnByEffect}/{evt.StatusStacksApplied}, " +
                $"summary='{evt.EffectSummary}', draw/hand/discard/exhaust={evt.DrawPileCount}/{evt.HandCount}/{evt.DiscardPileCount}/{evt.ExhaustPileCount}.");
        }

        private static void HandleBattleHandDiscarded(BattleHandDiscardedEvent evt)
        {
            Debug.Log(
                "Step11TestDriver Event: HandDiscarded " +
                $"node={evt.NodeId}, turn={evt.TurnNumber}, discarded={evt.DiscardedCount}, " +
                $"draw/hand/discard/exhaust={evt.DrawPileCount}/{evt.HandCount}/{evt.DiscardPileCount}/{evt.ExhaustPileCount}.");
        }

        private static void HandleBattleEnemyTurnResolved(BattleEnemyTurnResolvedEvent evt)
        {
            Debug.Log(
                "Step13TestDriver Event: EnemyTurnResolved " +
                $"node={evt.NodeId}, turn={evt.TurnNumber}, enemyCount={evt.EnemyCount}, " +
                $"result(dmg/armor/plunder)={evt.TotalDamageToPlayer}/{evt.TotalArmorGained}/{evt.TotalResourcesPlundered}, " +
                $"summary='{evt.Summary}', " +
                $"draw/hand/discard/exhaust={evt.DrawPileCount}/{evt.HandCount}/{evt.DiscardPileCount}/{evt.ExhaustPileCount}.");
        }

        private static void HandleBattleCardsDrawn(BattleCardsDrawnEvent evt)
        {
            Debug.Log(
                "Step11TestDriver Event: CardsDrawn " +
                $"node={evt.NodeId}, turn={evt.TurnNumber}, requested={evt.RequestedCount}, drawn={evt.DrawnCount}, reshuffle={evt.ReshuffleCount}, " +
                $"draw/hand/discard/exhaust={evt.DrawPileCount}/{evt.HandCount}/{evt.DiscardPileCount}/{evt.ExhaustPileCount}.");
        }

        private static void HandleBattleSettled(BattleSettledEvent evt)
        {
            string outcome = evt.IsVictory ? "VICTORY" : "DEFEAT";
            Debug.Log(
                $"Step14TestDriver Event: BattleSettled " +
                $"outcome={outcome}, node={evt.NodeId}, turn={evt.TurnNumber}, " +
                $"rewards=[{FormatResourceAmounts(evt.Rewards)}], " +
                $"lost=[{FormatResourceAmounts(evt.ResourcesLost)}], " +
                $"cardsDiscarded={evt.CardsDiscardedCount}, " +
                $"companionInjured={evt.CompanionInjured}, " +
                $"summary='{evt.SettlementSummary}'.");
        }

        private static string FormatResourceAmounts(IReadOnlyList<ResourceAmount> amounts)
        {
            if (amounts == null || amounts.Count == 0)
            {
                return "none";
            }

            string[] entries = new string[amounts.Count];
            for (int i = 0; i < amounts.Count; i++)
            {
                string sign = amounts[i].Amount >= 0 ? "+" : "";
                entries[i] = $"{sign}{amounts[i].Amount} {amounts[i].Type}";
            }

            return string.Join(", ", entries);
        }

        private static string FormatEnemyQueue(IReadOnlyList<EnemyConfig> queue)
        {
            if (queue == null || queue.Count == 0)
            {
                return "none";
            }

            string[] names = new string[queue.Count];
            for (int i = 0; i < queue.Count; i++)
            {
                EnemyConfig enemy = queue[i];
                names[i] = enemy == null ? "null" : $"{enemy.DisplayName} ({enemy.Id})";
            }

            return string.Join(", ", names);
        }

        private static string FormatEnemyIntents(IReadOnlyList<BattleEnemyIntentView> intents)
        {
            if (intents == null || intents.Count == 0)
            {
                return "none";
            }

            string[] entries = new string[intents.Count];
            for (int i = 0; i < intents.Count; i++)
            {
                BattleEnemyIntentView intent = intents[i];
                entries[i] = $"#{intent.EnemyIndex}:{intent.EnemyDisplayName}:{intent.IntentType}({intent.IntentValue})";
            }

            return string.Join(", ", entries);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || _context == null)
            {
                return;
            }

            const int width = 460;
            const int height = 220;
            Rect rect = new Rect(Screen.width - width - 16, 196, width, height);
            GUI.Box(rect, string.Empty);

            string encounterLine = _context.HasActiveJourneyEncounter
                ? $"Active: node {_context.ActiveJourneyNodeId} ({_context.ActiveJourneyNodeType})"
                : "Active: none";

            string status = "Step8 Test Driver\n" +
                            $"Food={_context.GetResource(ResourceType.Food)}  CurrentNode={_context.JourneyState.NodeIndex}  Visited={_context.JourneyState.NodesVisited}\n" +
                            encounterLine + "\n";

            IReadOnlyList<JourneyMapNode> nextNodes = _context.GetAvailableNextJourneyNodes();
            if (!_context.HasActiveJourneyEncounter && nextNodes.Count > 0)
            {
                status += "Next nodes: ";
                for (int i = 0; i < nextNodes.Count; i++)
                {
                    if (i > 0)
                    {
                        status += " | ";
                    }

                    JourneyMapNode node = nextNodes[i];
                    status += $"{node.Id}:{node.NodeType}";
                }
            }
            else if (!_context.HasActiveJourneyEncounter)
            {
                status += "Next nodes: none";
            }

            if (!string.IsNullOrWhiteSpace(_context.LastJourneyAdvanceBlockMessage))
            {
                status += "\nBlocked: " + _context.LastJourneyAdvanceBlockMessage;
            }

            if (_battleTurnController != null && _battleTurnController.IsActive)
            {
                status += "\n\nBattle Turn: " +
                          $"phase={_battleTurnController.Phase}, turn={_battleTurnController.TurnNumber}, " +
                          $"energy={_battleTurnController.CurrentEnergy}/{_battleTurnController.MaxEnergyPerTurn}, " +
                          $"draw/hand/discard/exhaust={_battleTurnController.DrawPile.Count}/" +
                          $"{_battleTurnController.Hand.Count}/{_battleTurnController.DiscardPile.Count}/{_battleTurnController.ExhaustPile.Count}";
            }

            GUI.Label(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, 108), status);

            float buttonTop = rect.y + 118f;
            if (_context.HasActiveJourneyEncounter)
            {
                if (GUI.Button(new Rect(rect.x + 8f, buttonTop, rect.width - 16f, 32f), $"Complete Node [{_completeNodeHotkey}]"))
                {
                    _context.TryCompleteActiveJourneyNode(out _);
                }
            }
            else
            {
                for (int i = 0; i < nextNodes.Count && i < 4; i++)
                {
                    JourneyMapNode node = nextNodes[i];
                    string label = $"Enter Node {node.Id} ({node.NodeType})";
                    if (GUI.Button(new Rect(rect.x + 8f, buttonTop + i * 34f, rect.width - 16f, 30f), label))
                    {
                        _context.TryEnterNextJourneyNode(node.Id, out _);
                    }
                }
            }

            GUI.Label(
                new Rect(rect.x + 8f, rect.y + rect.height - 26f, rect.width - 16f, 20f),
                $"[{_selectFirstNodeHotkey}/{_selectSecondNodeHotkey}] Select Next  [{_completeNodeHotkey}] Complete  [{_depleteFoodHotkey}] Food=0  [{_playCardHotkey}] Play  [{_endTurnHotkey}] EndTurn");
        }
    }
}
