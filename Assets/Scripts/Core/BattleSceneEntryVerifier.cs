using System;
using System.Collections.Generic;
using System.Text;
using OneManJourney.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BattleSceneEntryVerifier : MonoBehaviour
    {
        private GameContext _context;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            TryResolveContext();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            _context = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode _)
        {
            if (!TryResolveContext())
            {
                return;
            }

            if (_context.ActiveJourneyNodeType != JourneyNodeType.Battle
                && _context.ActiveJourneyNodeType != JourneyNodeType.Boss)
            {
                return;
            }

            if (!string.Equals(scene.name, _context.ActiveJourneySceneName, StringComparison.Ordinal))
            {
                return;
            }

            BattleEncounterConfig activeConfig = _context.ActiveBattleEncounterConfig;
            bool hasNodeConfig = _context.TryGetBattleNodeEncounterConfig(_context.ActiveJourneyNodeId, out BattleEncounterConfig nodeConfig);
            bool queueMatches = hasNodeConfig
                && activeConfig != null
                && HasSameEnemyQueue(nodeConfig.EnemyQueue, activeConfig.EnemyQueue);

            IReadOnlyList<EnemyConfig> nodeQueue = nodeConfig?.EnemyQueue;
            IReadOnlyList<EnemyConfig> activeQueue = activeConfig?.EnemyQueue;
            Debug.Log(
                "Step10Verifier: Battle entry loaded " +
                $"scene='{scene.name}', node={_context.ActiveJourneyNodeId}, type={_context.ActiveJourneyNodeType}, " +
                $"nodeConfig=[{FormatEnemyQueue(nodeQueue)}], activeQueue=[{FormatEnemyQueue(activeQueue)}], match={queueMatches}.");
        }

        private bool TryResolveContext()
        {
            if (_context != null)
            {
                return true;
            }

            _context = GameContext.Instance;
            if (_context != null)
            {
                return true;
            }

            return GameServices.TryResolve(out _context);
        }

        private static bool HasSameEnemyQueue(IReadOnlyList<EnemyConfig> left, IReadOnlyList<EnemyConfig> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (int index = 0; index < left.Count; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static string FormatEnemyQueue(IReadOnlyList<EnemyConfig> queue)
        {
            if (queue == null || queue.Count == 0)
            {
                return "none";
            }

            var builder = new StringBuilder();
            for (int index = 0; index < queue.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                EnemyConfig enemy = queue[index];
                if (enemy == null)
                {
                    builder.Append("null");
                    continue;
                }

                builder.Append(enemy.DisplayName);
                builder.Append(" (");
                builder.Append(enemy.Id);
                builder.Append(')');
            }

            return builder.ToString();
        }
    }
}
