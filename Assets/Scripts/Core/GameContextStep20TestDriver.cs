using System;
using OneManJourney.Data;
using UnityEngine;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameContextStep20TestDriver : MonoBehaviour
    {
        private static GameContextStep20TestDriver _instance;

        [Header("Supply Hotkeys")]
        [SerializeField] private KeyCode _buyFoodHotkey = KeyCode.F1;
        [SerializeField] private KeyCode _buyFood5Hotkey = KeyCode.F2;
        [SerializeField] private KeyCode _healCompanionHotkey = KeyCode.F3;
        [SerializeField] private KeyCode _injureCompanionHotkey = KeyCode.F4;
        [SerializeField] private KeyCode _selectNextHotkey = KeyCode.F5;
        [SerializeField] private KeyCode _selectPrevHotkey = KeyCode.F6;

        private GameContext _context;
        private int _selectedIndex;
        private string _lastMessage = string.Empty;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable() { TryBind(); }
        private void OnDisable() { _context = null; }

        private void Update()
        {
            if (!TryBind()) return;

            if (Input.GetKeyDown(_buyFoodHotkey)) BuyFood(1);
            if (Input.GetKeyDown(_buyFood5Hotkey)) BuyFood(5);
            if (Input.GetKeyDown(_healCompanionHotkey)) HealSelected();
            if (Input.GetKeyDown(_injureCompanionHotkey)) InjureSelected();
            if (Input.GetKeyDown(_selectNextHotkey)) CycleSelection(1);
            if (Input.GetKeyDown(_selectPrevHotkey)) CycleSelection(-1);
        }

        private bool TryBind()
        {
            if (_context == null)
            {
                _context = GameContext.Instance;
                if (_context == null) GameServices.TryResolve(out _context);
            }

            return _context != null;
        }

        private void BuyFood(int amount)
        {
            if (!IsAtSupplyNode())
            {
                _lastMessage = "Not at a Supply node.";
                return;
            }

            if (_context.TryBuyResource(ResourceType.Food, amount, out string msg))
                _lastMessage = msg;
            else
            {
                _lastMessage = $"Buy Food failed: {msg}";
                Debug.LogWarning($"Step20: {_lastMessage}");
            }
        }

        private void HealSelected()
        {
            if (!IsAtSupplyNode())
            {
                _lastMessage = "Not at a Supply node.";
                return;
            }

            CompanionState companion = GetSelectedCompanion();
            if (companion == null)
            {
                _lastMessage = "No companion selected.";
                return;
            }

            if (_context.TryHealCompanion(companion, out string msg))
                _lastMessage = msg;
            else
            {
                _lastMessage = $"Heal failed: {msg}";
                Debug.LogWarning($"Step20: {_lastMessage}");
            }
        }

        private bool IsAtSupplyNode()
        {
            return _context != null &&
                   _context.HasActiveJourneyEncounter &&
                   _context.ActiveJourneyNodeType == JourneyNodeType.Supply;
        }

        private void InjureSelected()
        {
            CompanionState companion = GetSelectedCompanion();
            if (companion == null)
            {
                _lastMessage = "No companion selected.";
                return;
            }

            if (_context.TryInjureCompanion(companion))
                _lastMessage = $"{companion.DisplayName} injured. HP: {companion.CurrentHealth}/{companion.MaxHealth}.";
            else
                _lastMessage = $"{companion.DisplayName} is already injured.";
        }

        private CompanionState GetSelectedCompanion()
        {
            int total = _context.ActiveCompanions.Count + _context.CompanionReserve.Count;
            if (total == 0) return null;

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, total - 1);

            int activeCount = _context.ActiveCompanions.Count;
            if (_selectedIndex < activeCount)
                return _context.ActiveCompanions[_selectedIndex];

            return _context.CompanionReserve[_selectedIndex - activeCount];
        }

        private void CycleSelection(int dir)
        {
            int total = _context.ActiveCompanions.Count + _context.CompanionReserve.Count;
            if (total == 0) return;

            _selectedIndex = (_selectedIndex + dir + total) % total;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || _context == null) return;

            const int width = 300;
            const int height = 340;
            Rect rect = new Rect(Screen.width - width - 16, 250, width, height);
            GUI.Box(rect, string.Empty);

            string status = "Step20 Supply Test Driver\n\n";
            bool atSupply = IsAtSupplyNode();
            status += atSupply ? "** AT SUPPLY NODE **\n\n" : "(not at supply node)\n\n";
            status += $"Wealth: {_context.GetResource(ResourceType.Wealth)}";
            status += $"  Medical: {_context.GetResource(ResourceType.MedicalSupplies)}\n";
            status += $"Food: {_context.GetResource(ResourceType.Food)}";

            int foodPrice = _context.GetTradePrice(ResourceType.Food, true);
            status += $"  (buy={foodPrice}/ea)\n\n";

            status += $"[{_buyFoodHotkey}] Buy Food x1  [{_buyFood5Hotkey}] Buy x5\n\n";
            status += $"Companions:\n";

            int activeCount = _context.ActiveCompanions.Count;
            int reserveCount = _context.CompanionReserve.Count;
            int total = activeCount + reserveCount;

            for (int i = 0; i < activeCount; i++)
            {
                CompanionState c = _context.ActiveCompanions[i];
                string marker = i == _selectedIndex ? " <<<" : "";
                status += $"  [{i}] {c.DisplayName} HP:{c.CurrentHealth}/{c.MaxHealth}{(c.IsInjured ? " INJURED" : "")}{marker}\n";
            }

            for (int i = 0; i < reserveCount; i++)
            {
                CompanionState c = _context.CompanionReserve[i];
                int gi = activeCount + i;
                string marker = gi == _selectedIndex ? " <<<" : "";
                status += $"  [{gi}] {c.DisplayName} HP:{c.CurrentHealth}/{c.MaxHealth}{(c.IsInjured ? " INJURED" : "")}{marker}\n";
            }

            if (total == 0) status += "  None\n";

            status += $"\n[{_healCompanionHotkey}] Heal (1 Med)  [{_injureCompanionHotkey}] Injure\n";
            status += $"[{_selectNextHotkey}]/[{_selectPrevHotkey}] Select\n";
            status += $"Medical cost: 1/heal\n";

            if (!string.IsNullOrWhiteSpace(_lastMessage))
                status += $"\nLast: {_lastMessage}";

            GUI.Label(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16), status);
        }
    }
}
