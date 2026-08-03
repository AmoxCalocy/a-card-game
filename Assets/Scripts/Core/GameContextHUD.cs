using System;
using System.Text;
using OneManJourney.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameContextHUD : MonoBehaviour
    {
        [SerializeField] private GameObject _uiPrefab;

        private GameContext _context;
        private BattleTurnController _battle;
        private GameEventBus _eventBus;
        private IDisposable _resourceSubscription;
        private IDisposable _turnStartedSubscription;
        private IDisposable _flowEndedSubscription;
        private IDisposable _nodeEnteredSubscription;
        private IDisposable _cardPlayedSubscription;
        private IDisposable _cardsDrawnSubscription;
        private IDisposable _handDiscardedSubscription;
        private TextMeshProUGUI _text;
        private readonly StringBuilder _builder = new StringBuilder(256);

        private void Awake()
        {
            EnsureUi();
            TryBind();
        }

        private void OnEnable() { TryBind(); }
        private void OnDisable() { Unbind(); }

        private void Update()
        {
            bool rebound = false;
            if (_context == null && TryBindContext()) rebound = true;
            if (_battle == null && TryBindBattle()) rebound = true;
            if (_eventBus == null && TryBindEventBus()) rebound = true;
            if (rebound) Refresh();
        }

        private bool TryBind()
        {
            bool bound = TryBindContext();
            bound |= TryBindBattle();
            bound |= TryBindEventBus();
            if (bound) Refresh();
            return _context != null;
        }

        private bool TryBindContext()
        {
            if (_context != null) return false;
            _context = GameContext.Instance;
            if (_context == null) GameServices.TryResolve(out _context);
            return _context != null;
        }

        private bool TryBindBattle()
        {
            if (_battle != null) return false;
            _battle = _context?.GetComponent<BattleTurnController>();
            if (_battle == null) GameServices.TryResolve(out _battle);
            return _battle != null;
        }

        private bool TryBindEventBus()
        {
            if (_eventBus != null) return false;
            _eventBus = _context?.EventBus;
            if (_eventBus == null) GameServices.TryResolve(out _eventBus);
            if (_eventBus == null) return false;

            _resourceSubscription = _eventBus.Subscribe<ResourceChangedEvent>(HandleResourceChanged);
            _turnStartedSubscription = _eventBus.Subscribe<BattleTurnStartedEvent>(HandleTurnStarted);
            _flowEndedSubscription = _eventBus.Subscribe<BattleFlowEndedEvent>(HandleFlowEnded);
            _nodeEnteredSubscription = _eventBus.Subscribe<JourneyNodeEnteredEvent>(HandleNodeEntered);
            _cardPlayedSubscription = _eventBus.Subscribe<BattleCardPlayedEvent>(HandleCardPlayed);
            _cardsDrawnSubscription = _eventBus.Subscribe<BattleCardsDrawnEvent>(HandleCardsDrawn);
            _handDiscardedSubscription = _eventBus.Subscribe<BattleHandDiscardedEvent>(HandleHandDiscarded);
            return true;
        }

        private void Unbind()
        {
            _resourceSubscription?.Dispose();
            _turnStartedSubscription?.Dispose();
            _flowEndedSubscription?.Dispose();
            _nodeEnteredSubscription?.Dispose();
            _cardPlayedSubscription?.Dispose();
            _cardsDrawnSubscription?.Dispose();
            _handDiscardedSubscription?.Dispose();
            _resourceSubscription = null;
            _turnStartedSubscription = null;
            _flowEndedSubscription = null;
            _nodeEnteredSubscription = null;
            _cardPlayedSubscription = null;
            _cardsDrawnSubscription = null;
            _handDiscardedSubscription = null;
            _eventBus = null;
            _battle = null;
            _context = null;
        }

        private void HandleResourceChanged(ResourceChangedEvent _) { Refresh(); }
        private void HandleTurnStarted(BattleTurnStartedEvent _) { Refresh(); }
        private void HandleFlowEnded(BattleFlowEndedEvent _) { Refresh(); }
        private void HandleNodeEntered(JourneyNodeEnteredEvent _) { Refresh(); }
        private void HandleCardPlayed(BattleCardPlayedEvent _) { Refresh(); }
        private void HandleCardsDrawn(BattleCardsDrawnEvent _) { Refresh(); }
        private void HandleHandDiscarded(BattleHandDiscardedEvent _) { Refresh(); }

        private void Refresh()
        {
            if (_text == null || _context == null) return;

            _builder.Clear();
            _builder.Append($"Food:{_context.GetResource(ResourceType.Food)}");
            _builder.Append($"  Wealth:{_context.GetResource(ResourceType.Wealth)}");
            _builder.Append($"  Rep:{_context.GetResource(ResourceType.Reputation)}");
            _builder.Append($"  Med:{_context.GetResource(ResourceType.MedicalSupplies)}");
            _builder.Append($"  Crisis:{_context.GetResource(ResourceType.Crisis)}");

            if (_context.HasActiveJourneyEncounter)
                _builder.Append($"  Node:{_context.ActiveJourneyNodeId}({_context.ActiveJourneyNodeType})");

            if (_battle != null && _battle.IsActive)
            {
                _builder.Append($"  Turn:{_battle.TurnNumber}");
                _builder.Append($"  E:{_battle.CurrentEnergy}/{_battle.MaxEnergyPerTurn}");
                _builder.Append($"  D:{_battle.DrawPile.Count}");
                _builder.Append($"  H:{_battle.Hand.Count}");
                _builder.Append($"  Dis:{_battle.DiscardPile.Count}");
            }

            _text.text = _builder.ToString();
        }

        private void EnsureUi()
        {
            if (_text != null) return;

#if UNITY_EDITOR
            if (_uiPrefab == null)
                _uiPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameContextHUD.prefab");
#endif

            if (_uiPrefab != null)
            {
                GameObject instance = Instantiate(_uiPrefab, transform, false);
                _text = instance.GetComponentInChildren<TextMeshProUGUI>();
                if (_text != null) return;
            }

            BuildUiProgrammatically();
        }

        private void BuildUiProgrammatically()
        {
            var canvasGo = new GameObject("GameContextHUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // Background bar
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(1f, 1f);
            bgRect.pivot = new Vector2(0.5f, 1f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(0f, 32f);
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.7f);

            // Text
            var textGo = new GameObject("HUDText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(bgGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(-16f, 0f);

            _text = textGo.GetComponent<TextMeshProUGUI>();
            _text.fontSize = 16f;
            _text.alignment = TextAlignmentOptions.MidlineLeft;
            _text.color = Color.white;
            _text.font = TMP_Settings.defaultFontAsset ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            Refresh();
        }
    }
}
