using System;
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
            if (_context != null)
            {
                return;
            }

            if (TryBindContext())
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
            return true;
        }

        private void UnbindContext()
        {
            if (_context == null)
            {
                return;
            }

            _context.Initialized -= HandleContextStateChanged;
            _context.StateChanged -= HandleContextStateChanged;
            _context = null;
        }

        private void HandleContextStateChanged()
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
            panelRect.sizeDelta = new Vector2(440f, 680f);

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
            _text.fontSize = 20f;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.color = Color.white;
            _text.enableWordWrapping = false;
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

            JourneyState state = _context.JourneyState;
            _builder.AppendLine($"Chapter: {state.Chapter}");
            _builder.AppendLine($"Node Index: {state.NodeIndex}");
            _builder.AppendLine($"Visited Nodes: {state.NodesVisited}");
            _builder.AppendLine($"Run Seed: {state.RunSeed}");
            _builder.AppendLine($"Crisis: {state.CrisisValue}");
            _builder.AppendLine($"Card Pool: {_context.CardPool.Count}");
            _builder.AppendLine($"Event Pool: {_context.EventPool.Count}");
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
    }
}
