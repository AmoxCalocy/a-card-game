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

        [Header("Hotkeys")]
        [SerializeField] private KeyCode _recruitNextHotkey = KeyCode.G;
        [SerializeField] private KeyCode _recruitAllHotkey = KeyCode.H;

        private GameContext _context;
        private GameEventBus _eventBus;
        private IDisposable _companionRecruitedSubscription;
        private int _nextRecruitIndex;
        private string _lastRecruitMessage = string.Empty;

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
            }

            return true;
        }

        private void Unbind()
        {
            _companionRecruitedSubscription?.Dispose();
            _companionRecruitedSubscription = null;
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
                _lastRecruitMessage = "No test companions assigned.";
                Debug.LogWarning($"Step15TestDriver Action: {_lastRecruitMessage}");
                return;
            }

            CompanionConfig companion = _testCompanions[_nextRecruitIndex % _testCompanions.Count];
            _nextRecruitIndex++;

            if (!_context.TryRecruitCompanion(companion, out string message))
            {
                _lastRecruitMessage = message;
                Debug.LogError($"Step15TestDriver Action: Recruit failed. {message}");
                return;
            }

            _lastRecruitMessage = $"Recruiting '{companion.DisplayName}'...";
        }

        private void TryRecruitAll()
        {
            if (_testCompanions.Count == 0)
            {
                _lastRecruitMessage = "No test companions assigned.";
                Debug.LogWarning($"Step15TestDriver Action: {_lastRecruitMessage}");
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

            _lastRecruitMessage = $"Recruit all: {recruited} succeeded, {failed} failed.";
            Debug.Log($"Step15TestDriver Action: {_lastRecruitMessage}");
        }

        private static void HandleCompanionRecruited(CompanionRecruitedEvent evt)
        {
            string squad = evt.AddedToActive ? "Active" : "Reserve";
            Debug.Log(
                "Step15TestDriver Event: CompanionRecruited " +
                $"companion='{evt.Companion.DisplayName}' ({evt.Companion.Role}), " +
                $"squad={squad}, " +
                $"active={evt.ActiveCompanionCount}/{GameContext.MaxActiveCompanions}, " +
                $"reserve={evt.ReserveCompanionCount}, " +
                $"cardsAdded={evt.StarterCardsAdded}, " +
                $"summary='{evt.Summary}'.");
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || _context == null)
            {
                return;
            }

            const int width = 400;
            const int height = 260;
            Rect rect = new Rect(Screen.width - width - 16, 16, width, height);
            GUI.Box(rect, string.Empty);

            string status = "Step15 Companion Test Driver\n";
            status += $"Active: {_context.ActiveCompanions.Count}/{GameContext.MaxActiveCompanions}";
            status += $"  Reserve: {_context.CompanionReserve.Count}\n";
            status += $"Card Pool: {_context.CardPool.Count}\n";

            if (_context.ActiveCompanions.Count > 0)
            {
                status += "Active Squad:\n";
                for (int i = 0; i < _context.ActiveCompanions.Count; i++)
                {
                    CompanionConfig c = _context.ActiveCompanions[i];
                    status += $"  [{i}] {c.DisplayName} ({c.Role})\n";
                }
            }

            if (_context.CompanionReserve.Count > 0)
            {
                status += "Reserve:\n";
                for (int i = 0; i < _context.CompanionReserve.Count; i++)
                {
                    CompanionConfig c = _context.CompanionReserve[i];
                    status += $"  [{i}] {c.DisplayName} ({c.Role})\n";
                }
            }

            if (!string.IsNullOrWhiteSpace(_lastRecruitMessage))
            {
                status += $"\nLast: {_lastRecruitMessage}";
            }

            GUI.Label(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, 150), status);

            float buttonTop = rect.y + 162f;
            string recruitLabel = _testCompanions.Count > 0
                ? $"Recruit Next: {GetNextCompanionName()} [{_recruitNextHotkey}]"
                : $"Recruit Next (none assigned) [{_recruitNextHotkey}]";
            if (GUI.Button(new Rect(rect.x + 8f, buttonTop, rect.width - 16f, 28f), recruitLabel))
            {
                TryRecruitNext();
            }

            if (GUI.Button(new Rect(rect.x + 8f, buttonTop + 32f, rect.width - 16f, 28f), $"Recruit All [{_recruitAllHotkey}]"))
            {
                TryRecruitAll();
            }

            GUI.Label(
                new Rect(rect.x + 8f, rect.y + height - 26f, rect.width - 16f, 20f),
                $"Test Companions: {_testCompanions.Count}  [{_recruitNextHotkey}] Recruit Next  [{_recruitAllHotkey}] Recruit All");
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
