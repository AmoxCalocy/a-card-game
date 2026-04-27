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

        private GameContext _context;
        private GameEventBus _eventBus;
        private IDisposable _journeyNodeEnteredSubscription;
        private IDisposable _journeyNodeCompletedSubscription;
        private IDisposable _journeyAdvanceBlockedSubscription;
        private IDisposable _battleEncounterPreparedSubscription;

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
            }

            return true;
        }

        private void Unbind()
        {
            _journeyNodeEnteredSubscription?.Dispose();
            _journeyNodeCompletedSubscription?.Dispose();
            _journeyAdvanceBlockedSubscription?.Dispose();
            _battleEncounterPreparedSubscription?.Dispose();
            _journeyNodeEnteredSubscription = null;
            _journeyNodeCompletedSubscription = null;
            _journeyAdvanceBlockedSubscription = null;
            _battleEncounterPreparedSubscription = null;
            _eventBus = null;
            _context = null;
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
                $"[{_selectFirstNodeHotkey}/{_selectSecondNodeHotkey}] Select Next  [{_completeNodeHotkey}] Complete  [{_depleteFoodHotkey}] Food=0");
        }
    }
}
