using System;
using System.Collections.Generic;
using OneManJourney.Data;
using UnityEngine;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameContextStep21TestDriver : MonoBehaviour
    {
        private static GameContextStep21TestDriver _instance;

        [Header("Hotkeys")]
        [SerializeField] private KeyCode _resolveOption1Hotkey = KeyCode.F1;
        [SerializeField] private KeyCode _resolveOption2Hotkey = KeyCode.F2;
        [SerializeField] private KeyCode _resolveOption3Hotkey = KeyCode.F3;
        [SerializeField] private KeyCode _cycleEventHotkey = KeyCode.F4;
        [SerializeField] private KeyCode _showEventHotkey = KeyCode.F5;

        private GameContext _context;
        private GameEventBus _eventBus;
        private IDisposable _eventResolvedSubscription;
        private int _eventIndex;
        private string _lastMessage = string.Empty;

        private readonly List<EventConfig> _availableEvents = new List<EventConfig>();

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadEvents();
        }

        private void OnEnable() { TryBind(); }
        private void OnDisable() { Unbind(); }

        private void Update()
        {
            if (!TryBind()) return;

            if (Input.GetKeyDown(_resolveOption1Hotkey)) ResolveOption(0);
            if (Input.GetKeyDown(_resolveOption2Hotkey)) ResolveOption(1);
            if (Input.GetKeyDown(_resolveOption3Hotkey)) ResolveOption(2);
            if (Input.GetKeyDown(_cycleEventHotkey)) CycleEvent();
            if (Input.GetKeyDown(_showEventHotkey)) LogCurrentEvent();
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

            if (_eventResolvedSubscription == null)
                _eventResolvedSubscription = _eventBus.Subscribe<EventResolvedEvent>(HandleEventResolved);

            return true;
        }

        private void Unbind()
        {
            _eventResolvedSubscription?.Dispose();
            _eventResolvedSubscription = null;
            _eventBus = null;
            _context = null;
        }

        private void ResolveOption(int optionIndex)
        {
            EventConfig evt = GetCurrentEvent();
            if (evt == null)
            {
                _lastMessage = "No events available.";
                return;
            }

            if (_context.TryResolveEvent(evt, optionIndex, out string summary))
                _lastMessage = $"OK: {summary}";
            else
            {
                _lastMessage = $"Failed: {summary}";
                Debug.LogWarning($"Step21: {_lastMessage}");
            }
        }

        private void CycleEvent()
        {
            if (_availableEvents.Count == 0) return;
            _eventIndex = (_eventIndex + 1) % _availableEvents.Count;
            _lastMessage = $"Event: {GetCurrentEvent()?.DisplayName} ({_eventIndex + 1}/{_availableEvents.Count})";
            Debug.Log($"Step21: {_lastMessage}");
            LogCurrentEvent();
        }

        private void LogCurrentEvent()
        {
            EventConfig evt = GetCurrentEvent();
            if (evt == null) return;

            Debug.Log($"Step21 Event: {evt.DisplayName} - {evt.Description}");
            for (int i = 0; i < evt.Options.Count; i++)
            {
                EventOptionData opt = evt.Options[i];
                Debug.Log($"  [{i}] {opt.Title} ({opt.ResolutionType}) chance={opt.SuccessChance:P0} costs={opt.Costs.Count} rewards={opt.Rewards.Count}");
            }
        }

        private EventConfig GetCurrentEvent()
        {
            if (_availableEvents.Count == 0) return null;
            _eventIndex = Mathf.Clamp(_eventIndex, 0, _availableEvents.Count - 1);
            return _availableEvents[_eventIndex];
        }

        private static void HandleEventResolved(EventResolvedEvent evt)
        {
            Debug.Log($"Step21 Event Result: event='{evt.EventConfig.DisplayName}' option='{evt.Option.Title}' type={evt.ResolutionType} success={evt.Success} summary='{evt.Summary}'.");
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || _context == null) return;

            const int width = 320;
            const int height = 360;
            Rect rect = new Rect(Screen.width - width - 16, 250, width, height);
            GUI.Box(rect, string.Empty);

            string status = "Step21 Event Test Driver\n\n";

            EventConfig evt = GetCurrentEvent();
            if (evt == null)
            {
                status += "No events available.\nCheck Assets/Data for EventConfig assets.";
            }
            else
            {
                status += $"Event {_eventIndex + 1}/{_availableEvents.Count}: {evt.DisplayName}\n";
                status += $"{evt.Description}\n\n";

                IReadOnlyList<EventOptionData> options = evt.Options;
                for (int i = 0; i < options.Count; i++)
                {
                    EventOptionData opt = options[i];
                    string hotkey = i switch { 0 => $"[{_resolveOption1Hotkey}]", 1 => $"[{_resolveOption2Hotkey}]", 2 => $"[{_resolveOption3Hotkey}]", _ => "" };
                    status += $"{hotkey} {opt.Title} ({opt.ResolutionType})\n";
                    if (opt.Costs.Count > 0)
                    {
                        status += "   Costs:";
                        for (int j = 0; j < opt.Costs.Count; j++)
                            status += $" {opt.Costs[j].Amount} {opt.Costs[j].Type}";
                        status += "\n";
                    }

                    if (opt.Rewards.Count > 0)
                    {
                        status += "   Rewards:";
                        for (int j = 0; j < opt.Rewards.Count; j++)
                            status += $" {opt.Rewards[j].Amount} {opt.Rewards[j].Type}";
                        status += "\n";
                    }

                    if (opt.ResolutionType == EventResolutionType.SkillCheck)
                        status += $"   Chance: {opt.SuccessChance:P0}\n";
                    if (opt.RequiredReputation > 0)
                        status += $"   Rep req: {opt.RequiredReputation}\n";
                    if (opt.SacrificeCardCount > 0)
                        status += $"   Sacrifice: {opt.SacrificeCardCount} card(s)\n";
                }
            }

            status += $"\n[{_cycleEventHotkey}] Cycle Event  [{_showEventHotkey}] Log Info\n";

            if (!string.IsNullOrWhiteSpace(_lastMessage))
                status += $"\nLast: {_lastMessage}";

            GUI.Label(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16), status);
        }

        private void LoadEvents()
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:EventConfig", new[] { "Assets/Data" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                EventConfig asset = UnityEditor.AssetDatabase.LoadAssetAtPath<EventConfig>(path);
                if (asset != null && !_availableEvents.Contains(asset))
                    _availableEvents.Add(asset);
            }
#endif
        }
    }
}
