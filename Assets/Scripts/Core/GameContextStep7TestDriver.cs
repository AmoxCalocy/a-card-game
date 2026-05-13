using System;
using UnityEngine;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameContextStep7TestDriver : MonoBehaviour
    {
        private static GameContextStep7TestDriver _instance;

        [Header("Hotkeys")]
        [SerializeField] private KeyCode _regenerateMapHotkey = KeyCode.G;
        [SerializeField] private KeyCode _logMapHotkey = KeyCode.M;

        private GameContext _context;
        private GameEventBus _eventBus;
        private IDisposable _journeyMapGeneratedSubscription;

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

            if (Input.GetKeyDown(_regenerateMapHotkey))
            {
                _context.RegenerateJourneyMap();
            }

            if (Input.GetKeyDown(_logMapHotkey))
            {
                LogMapDetails(_context.JourneyMap);
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

            if (_journeyMapGeneratedSubscription == null)
            {
                _journeyMapGeneratedSubscription = _eventBus.Subscribe<JourneyMapGeneratedEvent>(HandleMapGenerated);
            }

            return true;
        }

        private void Unbind()
        {
            _journeyMapGeneratedSubscription?.Dispose();
            _journeyMapGeneratedSubscription = null;
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

        private static void HandleMapGenerated(JourneyMapGeneratedEvent evt)
        {
            Debug.Log(
                "Step7TestDriver Event: MapGenerated " +
                $"seed={evt.Seed}, nodes={evt.NodeCount}, routes={evt.RouteCount}, branchingNodes={evt.BranchingNodeCount}, " +
                $"battle/event/supply/boss={evt.BattleNodeCount}/{evt.EventNodeCount}/{evt.SupplyNodeCount}/{evt.BossNodeCount}.");
        }

        private static void LogMapDetails(JourneyMap map)
        {
            if (map == null)
            {
                Debug.Log("Step7TestDriver: map is null.");
                return;
            }

            Debug.Log(
                "Step7TestDriver: map summary " +
                $"seed={map.Seed}, nodes={map.Nodes.Count}, routes={map.RouteCount}, branchingNodes={map.BranchingNodeCount}.");

            for (int i = 0; i < map.Nodes.Count; i++)
            {
                JourneyMapNode node = map.Nodes[i];
                Debug.Log(
                    $"Step7TestDriver: node#{node.Id} layer={node.LayerIndex} lane={node.LaneIndex} type={node.NodeType} " +
                    $"next=[{string.Join(",", node.NextNodeIds)}]");
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            const int width = 400;
            const int height = 72;
            Rect rect = new Rect(Screen.width - width - 16, 116, width, height);
            GUI.Box(rect, string.Empty);
            GUI.Label(
                new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16),
                "Step7 Test Driver\n" +
                $"[{_regenerateMapHotkey}] Regenerate Map  [{_logMapHotkey}] Log Full Map");
        }
    }
}
