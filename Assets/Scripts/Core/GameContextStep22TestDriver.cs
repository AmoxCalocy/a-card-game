using System;
using OneManJourney.Data;
using UnityEngine;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameContextStep22TestDriver : MonoBehaviour
    {
        private static GameContextStep22TestDriver _instance;

        [Header("Hotkeys")]
        [SerializeField] private KeyCode _skillCheckHotkey = KeyCode.F1;
        [SerializeField] private KeyCode _showBestHotkey = KeyCode.F2;

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

            if (Input.GetKeyDown(_skillCheckHotkey)) RunSkillCheck();
            if (Input.GetKeyDown(_showBestHotkey)) ShowBestCompanion();
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

        private void RunSkillCheck()
        {
            CompanionState best = _context.GetBestCompanionForCheck();
            if (best == null)
            {
                _lastMessage = "No companions. Roll d20 only.";
                int roll = UnityEngine.Random.Range(1, 21);
                int rep = _context.GetResource(ResourceType.Reputation);
                int dc = 12 + Mathf.RoundToInt(rep * 0.05f);
                _lastMessage += $" Roll={roll} vs DC={dc} (rep={rep}).";
                Debug.Log($"Step22: {_lastMessage}");
                return;
            }

            int rep2 = _context.GetResource(ResourceType.Reputation);
            int repReduction = Mathf.RoundToInt(rep2 * 0.03f);
            int dc2 = Mathf.Max(6, 12 - repReduction);
            _context.TryCompanionSkillCheck(best, dc2, out CompanionSkillCheckEvent result);
            _lastMessage = result.Success
                ? $"PASS: {best.DisplayName} roll={result.Roll}+{best.GetSkillCheckValue()}={result.Total} vs DC={dc2}"
                : $"FAIL: {best.DisplayName} roll={result.Roll}+{best.GetSkillCheckValue()}={result.Total} vs DC={dc2}";
            Debug.Log($"Step22 SkillCheck: {_lastMessage}");
        }

        private void ShowBestCompanion()
        {
            CompanionState best = _context.GetBestCompanionForCheck();
            if (best == null)
            {
                _lastMessage = "No companions available.";
                return;
            }

            int rep = _context.GetResource(ResourceType.Reputation);
            int dc = Mathf.Max(6, 12 - Mathf.RoundToInt(rep * 0.03f));
            _lastMessage = $"Best: {best.DisplayName} skill={best.GetSkillCheckValue()} (base:{best.SkillCheckBonus})";
            _lastMessage += $"\nReputation: {rep} -> DC={dc}";
            Debug.Log($"Step22 BestCompanion: {_lastMessage}");
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || _context == null) return;

            const int width = 300;
            const int height = 200;
            Rect rect = new Rect(Screen.width - width - 16, Screen.height - height - 720, width, height);
            GUI.Box(rect, string.Empty);

            string status = "Step22 Skill Check Driver\n\n";
            int rep = _context.GetResource(ResourceType.Reputation);
            int dc = Mathf.Max(6, 12 - Mathf.RoundToInt(rep * 0.03f));
            status += $"Reputation: {rep}  Base DC: {dc}\n";
            status += $"Companions: {_context.ActiveCompanions.Count} active\n";

            CompanionState best = _context.GetBestCompanionForCheck();
            if (best != null)
                status += $"Best: {best.DisplayName} skill={best.GetSkillCheckValue()}\n";

            status += $"\n[{_skillCheckHotkey}] Skill Check\n";
            status += $"[{_showBestHotkey}] Show Best\n\n";

            if (!string.IsNullOrWhiteSpace(_lastMessage))
                status += _lastMessage;

            GUI.Label(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16), status);
        }
    }
}
