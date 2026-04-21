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
    }
}
