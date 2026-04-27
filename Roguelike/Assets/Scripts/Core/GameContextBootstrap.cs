using UnityEngine;

namespace OneManJourney.Runtime
{
    public static class GameContextBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureContext()
        {
            GameContext context = FindContext();
            if (context == null)
            {
                GameObject root = new GameObject("GameContextRoot");
                context = root.AddComponent<GameContext>();
            }

            GameContextDebugPanel panel = FindPanel();
            if (panel == null)
            {
                context.gameObject.AddComponent<GameContextDebugPanel>();
            }

            JourneyNodeSceneRouter router = FindSceneRouter();
            if (router == null)
            {
                context.gameObject.AddComponent<JourneyNodeSceneRouter>();
            }

            BattleSceneEntryVerifier battleVerifier = FindBattleSceneEntryVerifier();
            if (battleVerifier == null)
            {
                context.gameObject.AddComponent<BattleSceneEntryVerifier>();
            }
        }

        private static GameContext FindContext()
        {
            GameContext[] items = Resources.FindObjectsOfTypeAll<GameContext>();
            if (items == null || items.Length == 0)
            {
                return null;
            }

            return items[0];
        }

        private static GameContextDebugPanel FindPanel()
        {
            GameContextDebugPanel[] items = Resources.FindObjectsOfTypeAll<GameContextDebugPanel>();
            if (items == null || items.Length == 0)
            {
                return null;
            }

            return items[0];
        }

        private static JourneyNodeSceneRouter FindSceneRouter()
        {
            JourneyNodeSceneRouter[] items = Resources.FindObjectsOfTypeAll<JourneyNodeSceneRouter>();
            if (items == null || items.Length == 0)
            {
                return null;
            }

            return items[0];
        }

        private static BattleSceneEntryVerifier FindBattleSceneEntryVerifier()
        {
            BattleSceneEntryVerifier[] items = Resources.FindObjectsOfTypeAll<BattleSceneEntryVerifier>();
            if (items == null || items.Length == 0)
            {
                return null;
            }

            return items[0];
        }
    }
}
