using System;
using System.Collections.Generic;
using OneManJourney.Data;
using UnityEngine;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameContextStep15TestDriver : MonoBehaviour
    {
        private static GameContextStep15TestDriver _instance;

        [Header("Test Companions")]
        [SerializeField] private List<CompanionConfig> _testCompanions = new List<CompanionConfig>();

        [Header("Hotkeys (Step 15)")]
        [SerializeField] private KeyCode _recruitNextHotkey = KeyCode.G;
        [SerializeField] private KeyCode _recruitAllHotkey = KeyCode.H;

        [Header("Hotkeys (Step 16)")]
        [SerializeField] private KeyCode _loyaltyUpHotkey = KeyCode.KeypadPlus;
        [SerializeField] private KeyCode _loyaltyDownHotkey = KeyCode.KeypadMinus;
        [SerializeField] private KeyCode _skillCheckHotkey = KeyCode.K;
        [SerializeField] private KeyCode _departureCheckHotkey = KeyCode.L;

        private GameContext _context;
        private GameEventBus _eventBus;
        private IDisposable _companionRecruitedSubscription;
        private IDisposable _companionLoyaltyChangedSubscription;
        private IDisposable _companionDepartureWarningSubscription;
        private IDisposable _companionDepartedSubscription;
        private IDisposable _companionSkillCheckSubscription;
        private int _nextRecruitIndex;
        private int _selectedCompanionIndex;
        private string _lastActionMessage = string.Empty;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCompanionConfigsIfNeeded();
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

            if (Input.GetKeyDown(_recruitNextHotkey))
            {
                TryRecruitNext();
            }

            if (Input.GetKeyDown(_recruitAllHotkey))
            {
                TryRecruitAll();
            }

            if (Input.GetKeyDown(_loyaltyUpHotkey))
            {
                ModifySelectedCompanionLoyalty(10);
            }

            if (Input.GetKeyDown(_loyaltyDownHotkey))
            {
                ModifySelectedCompanionLoyalty(-10);
            }

            if (Input.GetKeyDown(_skillCheckHotkey))
            {
                TrySkillCheckSelected();
            }

            if (Input.GetKeyDown(_departureCheckHotkey))
            {
                TryDepartureCheckSelected();
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _selectedCompanionIndex = Mathf.Max(0, _selectedCompanionIndex - 1);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                int total = _context.ActiveCompanions.Count + _context.CompanionReserve.Count;
                if (total > 0)
                {
                    _selectedCompanionIndex = Mathf.Min(total - 1, _selectedCompanionIndex + 1);
                }
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

            if (_companionRecruitedSubscription == null)
            {
                _companionRecruitedSubscription = _eventBus.Subscribe<CompanionRecruitedEvent>(HandleCompanionRecruited);
                _companionLoyaltyChangedSubscription = _eventBus.Subscribe<CompanionLoyaltyChangedEvent>(HandleCompanionLoyaltyChanged);
                _companionDepartureWarningSubscription = _eventBus.Subscribe<CompanionDepartureWarningEvent>(HandleCompanionDepartureWarning);
                _companionDepartedSubscription = _eventBus.Subscribe<CompanionDepartedEvent>(HandleCompanionDeparted);
                _companionSkillCheckSubscription = _eventBus.Subscribe<CompanionSkillCheckEvent>(HandleCompanionSkillCheck);
            }

            return true;
        }

        private void Unbind()
        {
            _companionRecruitedSubscription?.Dispose();
            _companionLoyaltyChangedSubscription?.Dispose();
            _companionDepartureWarningSubscription?.Dispose();
            _companionDepartedSubscription?.Dispose();
            _companionSkillCheckSubscription?.Dispose();
            _companionRecruitedSubscription = null;
            _companionLoyaltyChangedSubscription = null;
            _companionDepartureWarningSubscription = null;
            _companionDepartedSubscription = null;
            _companionSkillCheckSubscription = null;
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

        private void TryRecruitNext()
        {
            if (_testCompanions.Count == 0)
            {
                _lastActionMessage = "No test companions assigned.";
                Debug.LogWarning($"Step16TestDriver Action: {_lastActionMessage}");
                return;
            }

            CompanionConfig companion = _testCompanions[_nextRecruitIndex % _testCompanions.Count];
            _nextRecruitIndex++;

            if (!_context.TryRecruitCompanion(companion, out string message))
            {
                _lastActionMessage = message;
                Debug.LogError($"Step16TestDriver Action: Recruit failed. {message}");
                return;
            }

            _lastActionMessage = $"Recruiting '{companion.DisplayName}'...";
        }

        private void TryRecruitAll()
        {
            if (_testCompanions.Count == 0)
            {
                _lastActionMessage = "No test companions assigned.";
                Debug.LogWarning($"Step16TestDriver Action: {_lastActionMessage}");
                return;
            }

            int recruited = 0;
            int failed = 0;
            for (int i = 0; i < _testCompanions.Count; i++)
            {
                if (_context.TryRecruitCompanion(_testCompanions[i], out _))
                {
                    recruited++;
                }
                else
                {
                    failed++;
                }
            }

            _lastActionMessage = $"Recruit all: {recruited} succeeded, {failed} failed.";
            Debug.Log($"Step16TestDriver Action: {_lastActionMessage}");
        }

        private CompanionState GetSelectedCompanion()
        {
            int activeCount = _context.ActiveCompanions.Count;
            if (_selectedCompanionIndex < activeCount)
            {
                return _context.ActiveCompanions[_selectedCompanionIndex];
            }

            int reserveIndex = _selectedCompanionIndex - activeCount;
            if (reserveIndex >= 0 && reserveIndex < _context.CompanionReserve.Count)
            {
                return _context.CompanionReserve[reserveIndex];
            }

            _selectedCompanionIndex = 0;
            return activeCount > 0 ? _context.ActiveCompanions[0] : null;
        }

        private void ModifySelectedCompanionLoyalty(int delta)
        {
            CompanionState companion = GetSelectedCompanion();
            if (companion == null)
            {
                _lastActionMessage = "No companion selected.";
                return;
            }

            string reason = delta > 0 ? "Test loyalty increase" : "Test loyalty decrease";
            int actualDelta = _context.ModifyCompanionLoyalty(companion, delta, reason);
            _lastActionMessage = $"{companion.DisplayName} loyalty {actualDelta:+0;-#} (now {companion.CurrentLoyalty}).";
            Debug.Log($"Step16TestDriver Action: {_lastActionMessage} Reason: {reason}");
        }

        private void TrySkillCheckSelected()
        {
            CompanionState companion = GetSelectedCompanion();
            if (companion == null)
            {
                _lastActionMessage = "No companion selected.";
                return;
            }

            int difficulty = UnityEngine.Random.Range(8, 18);
            bool success = _context.TryCompanionSkillCheck(companion, difficulty, out CompanionSkillCheckEvent result);
            _lastActionMessage = success
                ? $"Skill check PASSED! roll={result.Roll} total={result.Total} vs DC={difficulty} (trait: {result.TraitUsed})"
                : $"Skill check FAILED. roll={result.Roll} total={result.Total} vs DC={difficulty} (trait: {result.TraitUsed})";
            Debug.Log($"Step16TestDriver Event: {_lastActionMessage}");
        }

        private void TryDepartureCheckSelected()
        {
            CompanionState companion = GetSelectedCompanion();
            if (companion == null)
            {
                _lastActionMessage = "No companion selected.";
                return;
            }

            bool departed = _context.CheckCompanionDeparture(companion, out string message);
            _lastActionMessage = departed
                ? message
                : $"{companion.DisplayName}: Departure check passed (Loyalty: {companion.CurrentLoyalty}, Risk: {companion.DepartureRisk:P0}).";
            Debug.Log($"Step16TestDriver Action: {_lastActionMessage}");
        }

        private static void HandleCompanionRecruited(CompanionRecruitedEvent evt)
        {
            string squad = evt.AddedToActive ? "Active" : "Reserve";
            Debug.Log(
                "Step16TestDriver Event: CompanionRecruited " +
                $"companion='{evt.Companion.DisplayName}' ({evt.Companion.Role}), " +
                $"squad={squad}, " +
                $"active={evt.ActiveCompanionCount}/{GameContext.MaxActiveCompanions}, " +
                $"reserve={evt.ReserveCompanionCount}, " +
                $"cardsAdded={evt.StarterCardsAdded}.");
        }

        private static void HandleCompanionLoyaltyChanged(CompanionLoyaltyChangedEvent evt)
        {
            Debug.Log(
                "Step16TestDriver Event: CompanionLoyaltyChanged " +
                $"companion='{evt.Companion.DisplayName}', " +
                $"loyalty {evt.PreviousLoyalty} -> {evt.NewLoyalty} ({(evt.Delta >= 0 ? "+" : "")}{evt.Delta}), " +
                $"reason='{evt.Reason}'.");
        }

        private static void HandleCompanionDepartureWarning(CompanionDepartureWarningEvent evt)
        {
            Debug.LogWarning(
                "Step16TestDriver Event: CompanionDepartureWarning " +
                $"companion='{evt.Companion.DisplayName}', " +
                $"risk={evt.DepartureRisk:P0}, " +
                $"message='{evt.WarningMessage}'.");
        }

        private static void HandleCompanionDeparted(CompanionDepartedEvent evt)
        {
            Debug.LogWarning(
                "Step16TestDriver Event: CompanionDeparted " +
                $"companion='{evt.Companion.DisplayName}', " +
                $"wasActive={evt.WasInActiveSquad}, " +
                $"reason='{evt.DepartureReason}'.");
        }

        private static void HandleCompanionSkillCheck(CompanionSkillCheckEvent evt)
        {
            string outcome = evt.Success ? "SUCCESS" : "FAILURE";
            Debug.Log(
                "Step16TestDriver Event: CompanionSkillCheck " +
                $"companion='{evt.Companion.DisplayName}', " +
                $"outcome={outcome}, " +
                $"roll={evt.Roll} total={evt.Total} vs DC={evt.Difficulty}, " +
                $"trait='{evt.TraitUsed}'.");
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || _context == null)
            {
                return;
            }

            const int width = 440;
            const int height = 400;
            Rect rect = new Rect(Screen.width - width - 16, 16, width, height);
            GUI.Box(rect, string.Empty);

            string status = "Step15+16 Companion Test Driver\n";
            status += $"Active: {_context.ActiveCompanions.Count}/{GameContext.MaxActiveCompanions}";
            status += $"  Reserve: {_context.CompanionReserve.Count}";
            status += $"  CardPool: {_context.CardPool.Count}\n";

            if (_context.ActiveCompanions.Count > 0)
            {
                status += "Active Squad:\n";
                for (int i = 0; i < _context.ActiveCompanions.Count; i++)
                {
                    CompanionState c = _context.ActiveCompanions[i];
                    string marker = i == _selectedCompanionIndex ? " <<<" : string.Empty;
                    status += $"  [{i}] {c.DisplayName} ({c.Role}) L:{c.CurrentLoyalty}({c.GetLoyaltyLabel()}) Risk:{c.DepartureRisk:P0}{marker}\n";
                }
            }

            if (_context.CompanionReserve.Count > 0)
            {
                status += "Reserve:\n";
                int offset = _context.ActiveCompanions.Count;
                for (int i = 0; i < _context.CompanionReserve.Count; i++)
                {
                    CompanionState c = _context.CompanionReserve[i];
                    int globalIndex = offset + i;
                    string marker = globalIndex == _selectedCompanionIndex ? " <<<" : string.Empty;
                    status += $"  [{globalIndex}] {c.DisplayName} ({c.Role}) L:{c.CurrentLoyalty}({c.GetLoyaltyLabel()}) Risk:{c.DepartureRisk:P0}{marker}\n";
                }
            }

            if (_context.ActiveCompanions.Count > 0 || _context.CompanionReserve.Count > 0)
            {
                CompanionState selected = GetSelectedCompanion();
                if (selected != null)
                {
                    status += $"\nSelected: {selected.DisplayName}\n";
                    status += $"  Loyalty: {selected.CurrentLoyalty}/100 ({selected.GetLoyaltyLabel()})\n";
                    status += $"  SkillCheck: {selected.GetSkillCheckValue()} (base:{selected.SkillCheckBonus})\n";
                    status += $"  Traits: {(selected.TraitIds.Count > 0 ? string.Join(", ", selected.TraitIds) : "None")}\n";
                }
            }

            if (!string.IsNullOrWhiteSpace(_lastActionMessage))
            {
                status += $"\nLast: {_lastActionMessage}";
            }

            GUI.Label(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, 260), status);

            float buttonTop = rect.y + 272f;
            float buttonWidth = (rect.width - 24f) / 2f;

            if (GUI.Button(new Rect(rect.x + 8f, buttonTop, buttonWidth, 24f), $"Recruit Next [{_recruitNextHotkey}]"))
            {
                TryRecruitNext();
            }

            if (GUI.Button(new Rect(rect.x + 12f + buttonWidth, buttonTop, buttonWidth, 24f), $"Recruit All [{_recruitAllHotkey}]"))
            {
                TryRecruitAll();
            }

            buttonTop += 28f;
            if (GUI.Button(new Rect(rect.x + 8f, buttonTop, buttonWidth, 24f), "Loyalty +10 [+]"))
            {
                ModifySelectedCompanionLoyalty(10);
            }

            if (GUI.Button(new Rect(rect.x + 12f + buttonWidth, buttonTop, buttonWidth, 24f), "Loyalty -10 [-]"))
            {
                ModifySelectedCompanionLoyalty(-10);
            }

            buttonTop += 28f;
            if (GUI.Button(new Rect(rect.x + 8f, buttonTop, buttonWidth, 24f), $"Skill Check [{_skillCheckHotkey}]"))
            {
                TrySkillCheckSelected();
            }

            if (GUI.Button(new Rect(rect.x + 12f + buttonWidth, buttonTop, buttonWidth, 24f), $"Departure Check [{_departureCheckHotkey}]"))
            {
                TryDepartureCheckSelected();
            }

            buttonTop += 28f;
            GUI.Label(
                new Rect(rect.x + 8f, buttonTop, rect.width - 16f, 20f),
                $"Select: [1]/[2] scroll  |  {_testCompanions.Count} companions loaded");
        }

        private string GetNextCompanionName()
        {
            if (_testCompanions.Count == 0)
            {
                return "none";
            }

            CompanionConfig companion = _testCompanions[_nextRecruitIndex % _testCompanions.Count];
            return companion == null ? "null" : companion.DisplayName;
        }

        private void LoadCompanionConfigsIfNeeded()
        {
            if (_testCompanions.Count > 0)
            {
                return;
            }

#if UNITY_EDITOR
            _testCompanions = LoadCompanionConfigsFromDataFolder();
#endif
        }

#if UNITY_EDITOR
        private static List<CompanionConfig> LoadCompanionConfigsFromDataFolder()
        {
            string[] folder = { "Assets/Data" };
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CompanionConfig", folder);
            if (guids.Length == 0)
            {
                guids = UnityEditor.AssetDatabase.FindAssets("t:CompanionConfig", new[] { "Assets" });
            }

            List<CompanionConfig> results = new List<CompanionConfig>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                CompanionConfig asset = UnityEditor.AssetDatabase.LoadAssetAtPath<CompanionConfig>(path);
                if (asset == null || results.Contains(asset))
                {
                    continue;
                }

                results.Add(asset);
            }

            return results;
        }
#endif
    }
}
