using System;
using OneManJourney.Data;
using UnityEngine;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameContextStep6TestDriver : MonoBehaviour
    {
        [Header("Hotkeys")]
        [SerializeField] private KeyCode _resourceHotkey = KeyCode.R;
        [SerializeField] private KeyCode _crisisHotkey = KeyCode.C;
        [SerializeField] private KeyCode _advanceNodeHotkey = KeyCode.N;
        [SerializeField] private KeyCode _drawCardHotkey = KeyCode.D;

        [Header("Values")]
        [SerializeField] private ResourceType _resourceType = ResourceType.Wealth;
        [SerializeField] private int _resourceDelta = 5;
        [SerializeField] private int _crisisDelta = 1;

        private GameContext _context;
        private GameEventBus _eventBus;
        private IDisposable _resourceChangedSubscription;
        private IDisposable _nodeSelectedSubscription;
        private IDisposable _cardDrawnSubscription;
        private IDisposable _contextInitializedSubscription;

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

            if (Input.GetKeyDown(_resourceHotkey))
            {
                _context.AddResource(_resourceType, _resourceDelta);
            }

            if (Input.GetKeyDown(_crisisHotkey))
            {
                _context.AddResource(ResourceType.Crisis, _crisisDelta);
            }

            if (Input.GetKeyDown(_advanceNodeHotkey))
            {
                _context.AdvanceNode();
            }

            if (Input.GetKeyDown(_drawCardHotkey))
            {
                if (!_context.TryDrawCard(out CardConfig card))
                {
                    Debug.Log("Step6TestDriver: draw failed, card pool is empty.");
                    return;
                }

                Debug.Log($"Step6TestDriver: drew card {card.DisplayName} ({card.Id}).");
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

            if (_resourceChangedSubscription == null)
            {
                _resourceChangedSubscription = _eventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
                _nodeSelectedSubscription = _eventBus.Subscribe<NodeSelectedEvent>(OnNodeSelected);
                _cardDrawnSubscription = _eventBus.Subscribe<CardDrawnEvent>(OnCardDrawn);
                _contextInitializedSubscription = _eventBus.Subscribe<GameContextInitializedEvent>(OnContextInitialized);
            }

            return true;
        }

        private void Unbind()
        {
            _resourceChangedSubscription?.Dispose();
            _nodeSelectedSubscription?.Dispose();
            _cardDrawnSubscription?.Dispose();
            _contextInitializedSubscription?.Dispose();

            _resourceChangedSubscription = null;
            _nodeSelectedSubscription = null;
            _cardDrawnSubscription = null;
            _contextInitializedSubscription = null;
            _eventBus = null;
            _context = null;
        }

        private static void OnResourceChanged(ResourceChangedEvent evt)
        {
            Debug.Log($"Step6TestDriver Event: ResourceChanged {evt.ResourceType} {evt.PreviousValue} -> {evt.CurrentValue} (delta {evt.Delta}).");
        }

        private static void OnNodeSelected(NodeSelectedEvent evt)
        {
            Debug.Log($"Step6TestDriver Event: NodeSelected chapter {evt.PreviousChapter}->{evt.CurrentChapter}, node {evt.PreviousNodeIndex}->{evt.CurrentNodeIndex}, visited {evt.PreviousNodesVisited}->{evt.CurrentNodesVisited}.");
        }

        private static void OnCardDrawn(CardDrawnEvent evt)
        {
            string cardName = evt.Card == null ? "null" : evt.Card.DisplayName;
            Debug.Log($"Step6TestDriver Event: CardDrawn {cardName}, remaining pool {evt.RemainingCardPoolCount}.");
        }

        private static void OnContextInitialized(GameContextInitializedEvent evt)
        {
            Debug.Log($"Step6TestDriver Event: ContextInitialized chapter={evt.Chapter}, node={evt.NodeIndex}, visited={evt.NodesVisited}, crisis={evt.CrisisValue}, cards={evt.CardPoolCount}, events={evt.EventPoolCount}.");
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            const int width = 460;
            const int height = 92;
            Rect rect = new Rect(Screen.width - width - 16, 16, width, height);
            GUI.Box(rect, string.Empty);
            GUI.Label(
                new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16),
                $"Step6 Test Driver\n" +
                $"[{_resourceHotkey}] {_resourceType} {_resourceDelta:+#;-#;0}  " +
                $"[{_crisisHotkey}] Crisis {_crisisDelta:+#;-#;0}  " +
                $"[{_advanceNodeHotkey}] Advance Node  " +
                $"[{_drawCardHotkey}] Draw Card");
        }
    }
}
