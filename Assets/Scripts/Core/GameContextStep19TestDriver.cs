using System;
using OneManJourney.Data;
using UnityEngine;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameContextStep19TestDriver : MonoBehaviour
    {
        private static GameContextStep19TestDriver _instance;

        [Header("Hotkeys")]
        [SerializeField] private KeyCode _buyFoodHotkey = KeyCode.F1;
        [SerializeField] private KeyCode _sellFoodHotkey = KeyCode.F2;
        [SerializeField] private KeyCode _buyMedicalHotkey = KeyCode.F3;
        [SerializeField] private KeyCode _sellMedicalHotkey = KeyCode.F4;
        [SerializeField] private KeyCode _checkPricesHotkey = KeyCode.F5;

        [Header("Amount")]
        [SerializeField] private int _tradeAmount = 1;

        private GameContext _context;
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

            if (Input.GetKeyDown(_buyFoodHotkey)) TryBuy(ResourceType.Food);
            if (Input.GetKeyDown(_sellFoodHotkey)) TrySell(ResourceType.Food);
            if (Input.GetKeyDown(_buyMedicalHotkey)) TryBuy(ResourceType.MedicalSupplies);
            if (Input.GetKeyDown(_sellMedicalHotkey)) TrySell(ResourceType.MedicalSupplies);
            if (Input.GetKeyDown(_checkPricesHotkey)) ShowPrices();
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

        private void TryBuy(ResourceType type)
        {
            if (_context.TryBuyResource(type, _tradeAmount, out string msg))
            {
                _lastMessage = msg;
            }
            else
            {
                _lastMessage = $"Buy failed: {msg}";
                Debug.LogWarning($"Step19: {_lastMessage}");
            }
        }

        private void TrySell(ResourceType type)
        {
            if (_context.TrySellResource(type, _tradeAmount, out string msg))
            {
                _lastMessage = msg;
            }
            else
            {
                _lastMessage = $"Sell failed: {msg}";
                Debug.LogWarning($"Step19: {_lastMessage}");
            }
        }

        private void ShowPrices()
        {
            ShowPrice(ResourceType.Food);
            ShowPrice(ResourceType.MedicalSupplies);
            ShowPrice(ResourceType.BuildingMaterials);
            ShowPrice(ResourceType.Intel);
            ShowPrice(ResourceType.DraftOrder);
        }

        private void ShowPrice(ResourceType type)
        {
            int buyPrice = _context.GetTradePrice(type, true);
            int sellPrice = _context.GetTradePrice(type, false);
            int reputation = _context.GetResource(ResourceType.Reputation);
            Debug.Log($"Step19 Prices (Rep={reputation}): {type} buy={buyPrice} sell={sellPrice}.");
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || _context == null) return;

            const int width = 300;
            const int height = 320;
            Rect rect = new Rect(Screen.width - width - 16, (Screen.height - height) / 2f - 100, width, height);
            GUI.Box(rect, string.Empty);

            string status = "Step19 Trade Test Driver\n\n";
            status += $"Wealth: {_context.GetResource(ResourceType.Wealth)}\n";
            status += $"Reputation: {_context.GetResource(ResourceType.Reputation)}\n\n";

            status += $"Food: {_context.GetResource(ResourceType.Food)}";
            int foodB = _context.GetTradePrice(ResourceType.Food, true);
            int foodS = _context.GetTradePrice(ResourceType.Food, false);
            status += $"  buy={foodB} sell={foodS}\n";
            status += $"  [F1] Buy  [{_sellFoodHotkey}] Sell\n\n";

            status += $"Medical: {_context.GetResource(ResourceType.MedicalSupplies)}";
            int medB = _context.GetTradePrice(ResourceType.MedicalSupplies, true);
            int medS = _context.GetTradePrice(ResourceType.MedicalSupplies, false);
            status += $"  buy={medB} sell={medS}\n";
            status += $"  [{_buyMedicalHotkey}] Buy  [{_sellMedicalHotkey}] Sell\n\n";

            status += $"[{_checkPricesHotkey}] Log all prices\n";
            status += $"Amount: {_tradeAmount}\n\n";

            if (!string.IsNullOrWhiteSpace(_lastMessage))
            {
                status += $"Last: {_lastMessage}";
            }

            GUI.Label(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16), status);
        }
    }
}
