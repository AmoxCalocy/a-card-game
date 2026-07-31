using System;
using System.Collections.Generic;
using OneManJourney.Data;
using UnityEngine;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameContextStep18TestDriver : MonoBehaviour
    {
        private static GameContextStep18TestDriver _instance;

        [Header("Hotkeys")]
        [SerializeField] private KeyCode _addFoodHotkey = KeyCode.F1;
        [SerializeField] private KeyCode _subFoodHotkey = KeyCode.F2;
        [SerializeField] private KeyCode _addWealthHotkey = KeyCode.F3;
        [SerializeField] private KeyCode _subWealthHotkey = KeyCode.F4;
        [SerializeField] private KeyCode _addReputationHotkey = KeyCode.F5;
        [SerializeField] private KeyCode _subReputationHotkey = KeyCode.F6;
        [SerializeField] private KeyCode _addMedicalHotkey = KeyCode.F7;
        [SerializeField] private KeyCode _subMedicalHotkey = KeyCode.F8;
        [SerializeField] private KeyCode _testClampHotkey = KeyCode.F9;
        [SerializeField] private KeyCode _testCrisisNegativeHotkey = KeyCode.F10;

        [Header("Test Values")]
        [SerializeField] private int _deltaAmount = 10;
        [SerializeField] private int _clampTestAmount = 5;

        private GameContext _context;
        private GameEventBus _eventBus;
        private IDisposable _resourceChangedSubscription;
        private readonly List<string> _recentLogs = new List<string>();
        private const int MaxLogLines = 8;

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

            if (Input.GetKeyDown(_addFoodHotkey)) ModifyResource(ResourceType.Food, _deltaAmount);
            if (Input.GetKeyDown(_subFoodHotkey)) ModifyResource(ResourceType.Food, -_deltaAmount);
            if (Input.GetKeyDown(_addWealthHotkey)) ModifyResource(ResourceType.Wealth, _deltaAmount);
            if (Input.GetKeyDown(_subWealthHotkey)) ModifyResource(ResourceType.Wealth, -_deltaAmount);
            if (Input.GetKeyDown(_addReputationHotkey)) ModifyResource(ResourceType.Reputation, _deltaAmount);
            if (Input.GetKeyDown(_subReputationHotkey)) ModifyResource(ResourceType.Reputation, -_deltaAmount);
            if (Input.GetKeyDown(_addMedicalHotkey)) ModifyResource(ResourceType.MedicalSupplies, _deltaAmount);
            if (Input.GetKeyDown(_subMedicalHotkey)) ModifyResource(ResourceType.MedicalSupplies, -_deltaAmount);

            if (Input.GetKeyDown(_testClampHotkey))
            {
                TestNegativeClamp();
            }

            if (Input.GetKeyDown(_testCrisisNegativeHotkey))
            {
                TestCrisisNegative();
            }
        }

        private bool TryBind()
        {
            if (_context == null)
            {
                _context = GameContext.Instance;
                if (_context == null) GameServices.TryResolve(out _context);
            }

            if (_eventBus == null)
            {
                if (_context != null) _eventBus = _context.EventBus;
                if (_eventBus == null) GameServices.TryResolve(out _eventBus);
            }

            if (_context == null || _eventBus == null) return false;

            if (_resourceChangedSubscription == null)
            {
                _resourceChangedSubscription = _eventBus.Subscribe<ResourceChangedEvent>(HandleResourceChanged);
            }

            return true;
        }

        private void Unbind()
        {
            _resourceChangedSubscription?.Dispose();
            _resourceChangedSubscription = null;
            _eventBus = null;
            _context = null;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void ModifyResource(ResourceType type, int delta)
        {
            int before = _context.GetResource(type);
            _context.AddResource(type, delta);
            int after = _context.GetResource(type);
            Debug.Log($"Step18 Action: {type} {before} -> {after} (delta={delta}).");
        }

        private void TestNegativeClamp()
        {
            int before = _context.GetResource(ResourceType.Food);
            _context.SetResource(ResourceType.Food, _clampTestAmount);
            LogToPanel($"Set Food to {_clampTestAmount} (was {before})");

            _context.AddResource(ResourceType.Food, -(_clampTestAmount + 5));
            int after = _context.GetResource(ResourceType.Food);
            string result = after == 0 ? "PASS" : "FAIL";
            LogToPanel($"Clamp test: Food -{_clampTestAmount + 5} -> {after} [{result}]");
            Debug.Log($"Step18 Clamp Test [{result}]: Food set to {_clampTestAmount}, then -{_clampTestAmount + 5} = {after} (expected 0).");
        }

        private void TestCrisisNegative()
        {
            int before = _context.GetResource(ResourceType.Crisis);
            _context.SetResource(ResourceType.Crisis, -10);
            int after = _context.GetResource(ResourceType.Crisis);
            string result = after == -10 ? "PASS" : "FAIL";
            LogToPanel($"Crisis negative test: {before} -> -10 -> {after} [{result}]");
            Debug.Log($"Step18 Crisis Negative Test [{result}]: Crisis set to -10 = {after} (expected -10, negative allowed).");

            _context.SetResource(ResourceType.Crisis, before);
        }

        private void LogToPanel(string msg)
        {
            _recentLogs.Add(msg);
            while (_recentLogs.Count > MaxLogLines) _recentLogs.RemoveAt(0);
        }

        private void HandleResourceChanged(ResourceChangedEvent evt)
        {
            LogToPanel($"{evt.ResourceType}: {evt.PreviousValue} -> {evt.CurrentValue} ({(evt.Delta >= 0 ? "+" : "")}{evt.Delta})");
            Debug.Log($"Step18 Event: ResourceChanged {evt.ResourceType} {evt.PreviousValue}->{evt.CurrentValue} delta={evt.Delta}.");
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || _context == null) return;

            const int width = 320;
            const int height = 360;
            Rect rect = new Rect(Screen.width - width - 16, Screen.height - height - 16, width, height);
            GUI.Box(rect, string.Empty);

            string status = "Step18 Resource Test Driver\n\n";
            status += $"Food: {_context.GetResource(ResourceType.Food)}  [{_addFoodHotkey}]/[F2]\n";
            status += $"Wealth: {_context.GetResource(ResourceType.Wealth)}  [{_addWealthHotkey}]/[F4]\n";
            status += $"Reputation: {_context.GetResource(ResourceType.Reputation)}  [{_addReputationHotkey}]/[F6]\n";
            status += $"Medical: {_context.GetResource(ResourceType.MedicalSupplies)}  [{_addMedicalHotkey}]/[F8]\n";
            status += $"Crisis: {_context.GetResource(ResourceType.Crisis)}\n";
            status += $"\n[{_testClampHotkey}] Clamp test  [{_testCrisisNegativeHotkey}] Crisis negative test\n";

            if (_recentLogs.Count > 0)
            {
                status += "\nRecent:\n";
                for (int i = 0; i < _recentLogs.Count; i++)
                {
                    status += $"  {_recentLogs[i]}\n";
                }
            }

            GUI.Label(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16), status);
        }
    }
}
