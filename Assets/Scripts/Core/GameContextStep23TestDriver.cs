using System;
using OneManJourney.Data;
using UnityEngine;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameContextStep23TestDriver : MonoBehaviour
    {
        private static GameContextStep23TestDriver _instance;

        [Header("Hotkeys")]
        [SerializeField] private KeyCode _showDisasterInfoHotkey = KeyCode.F1;
        [SerializeField] private KeyCode _triggerDisasterHotkey = KeyCode.F2;

        private GameContext _context;
        private GameEventBus _eventBus;
        private IDisposable _disasterSubscription;
        private string _lastMessage = string.Empty;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable() { TryBind(); }
        private void OnDisable() { Unbind(); }

        private void Update()
        {
            if (!TryBind()) return;

            if (Input.GetKeyDown(_showDisasterInfoHotkey)) ShowDisasterInfo();
            if (Input.GetKeyDown(_triggerDisasterHotkey)) TriggerDisaster();
        }

        private bool TryBind()
        {
            if (_context == null)
            {
                _context = GameContext.Instance;
                if (_context == null) GameServices.TryResolve(out _context);
            }

            if (_eventBus == null && _context != null) _eventBus = _context.EventBus;
            if (_eventBus == null) GameServices.TryResolve(out _eventBus);
            if (_context == null || _eventBus == null) return false;

            if (_disasterSubscription == null)
                _disasterSubscription = _eventBus.Subscribe<CrisisDisasterTriggeredEvent>(HandleDisasterTriggered);

            return true;
        }

        private void Unbind()
        {
            _disasterSubscription?.Dispose();
            _disasterSubscription = null;
            _eventBus = null;
            _context = null;
        }

        private void ShowDisasterInfo()
        {
            int crisis = _context.GetResource(ResourceType.Crisis);
            int threshold = _context.DisasterTriggerThreshold;
            int step = _context.DisasterTriggerStep;
            int next = _context.NextDisasterTriggerThreshold;
            string pending = _context.PendingDisasterEvent != null
                ? $"{_context.PendingDisasterEvent.DisplayName} ({_context.PendingDisasterType})"
                : "None";
            string lastMsg = _context.LastDisasterTriggerMessage ?? "None";

            _lastMessage = $"Crisis:{crisis} Threshold:{threshold} Step:{step} Next:{next}\nPending:{pending}\nLast:{lastMsg}";
            Debug.Log($"Step23 Info: {_lastMessage}");
        }

        private void TriggerDisaster()
        {
            int step = _context.DisasterTriggerStep;
            int next = _context.NextDisasterTriggerThreshold;
            int needed = next - _context.GetResource(ResourceType.Crisis);
            if (needed <= 0) needed = 1;

            int beforePool = _context.CardPool.Count;
            _context.AddResource(ResourceType.Crisis, needed);
            int afterPool = _context.CardPool.Count;

            _lastMessage = $"Added {needed} crisis -> triggered. Card pool: {beforePool} -> {afterPool} (+{afterPool - beforePool})";
            Debug.Log($"Step23 Action: {_lastMessage}");
        }

        private void HandleDisasterTriggered(CrisisDisasterTriggeredEvent evt)
        {
            string eventName = evt.DisasterEvent != null ? evt.DisasterEvent.DisplayName : "None";
            Debug.Log($"Step23 Event: CrisisDisasterTriggered crisis={evt.CrisisValue} threshold={evt.TriggerThreshold} type={evt.DisasterType} event='{eventName}' fallback={evt.UsedFallbackEvent}.");
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || _context == null) return;

            const int width = 320;
            const int height = 240;
            Rect rect = new Rect(Screen.width - width - 16, Screen.height - height - 16, width, height);
            GUI.Box(rect, string.Empty);

            string status = "Step23 Disaster Test Driver\n\n";
            status += $"Crisis: {_context.GetResource(ResourceType.Crisis)}\n";
            status += $"Next Trigger: {_context.NextDisasterTriggerThreshold}\n";
            status += $"Threshold: {_context.DisasterTriggerThreshold}  Step: {_context.DisasterTriggerStep}\n";
            status += $"Card Pool: {_context.CardPool.Count}\n";

            string pending = _context.PendingDisasterEvent != null
                ? _context.PendingDisasterEvent.DisplayName
                : "None";
            status += $"Pending: {pending} ({_context.PendingDisasterType})\n";
            status += !string.IsNullOrWhiteSpace(_context.LastDisasterTriggerMessage)
                ? $"Last: {_context.LastDisasterTriggerMessage}\n"
                : "Last: None\n";

            status += $"\n[{_showDisasterInfoHotkey}] Info  [{_triggerDisasterHotkey}] Trigger\n";

            if (!string.IsNullOrWhiteSpace(_lastMessage))
                status += $"\n{_lastMessage}";

            GUI.Label(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16), status);
        }
    }
}
