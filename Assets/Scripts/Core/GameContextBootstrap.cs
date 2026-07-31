using UnityEngine;
using UnityEngine.EventSystems;

namespace OneManJourney.Runtime
{
    public static class GameContextBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureContext()
        {
            EnsureEventSystem();

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

            BattleTurnController battleTurnController = FindBattleTurnController();
            if (battleTurnController == null)
            {
                context.gameObject.AddComponent<BattleTurnController>();
            }

            GameContextStep18TestDriver step18Driver = FindStep18Driver();
            if (step18Driver == null)
            {
                context.gameObject.AddComponent<GameContextStep18TestDriver>();
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

        private static BattleTurnController FindBattleTurnController()
        {
            BattleTurnController[] items = Resources.FindObjectsOfTypeAll<BattleTurnController>();
            if (items == null || items.Length == 0)
            {
                return null;
            }

            return items[0];
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(eventSystemObject);
        }

        private static GameContextStep18TestDriver FindStep18Driver()
        {
            GameContextStep18TestDriver[] items = Resources.FindObjectsOfTypeAll<GameContextStep18TestDriver>();
            if (items == null || items.Length == 0)
            {
                return null;
            }

            return items[0];
        }

    }
}
