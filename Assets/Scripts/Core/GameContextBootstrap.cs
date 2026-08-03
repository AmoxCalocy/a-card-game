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

            GameContextHUD hud = FindHUD();
            if (hud == null)
            {
                context.gameObject.AddComponent<GameContextHUD>();
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

            GameContextStep19TestDriver step19Driver = FindStep19Driver();
            if (step19Driver == null)
            {
                context.gameObject.AddComponent<GameContextStep19TestDriver>();
            }

            GameContextStep20TestDriver step20Driver = FindStep20Driver();
            if (step20Driver == null)
            {
                context.gameObject.AddComponent<GameContextStep20TestDriver>();
            }

            GameContextStep21TestDriver step21Driver = FindStep21Driver();
            if (step21Driver == null)
            {
                context.gameObject.AddComponent<GameContextStep21TestDriver>();
            }

            GameContextStep22TestDriver step22Driver = FindStep22Driver();
            if (step22Driver == null)
            {
                context.gameObject.AddComponent<GameContextStep22TestDriver>();
            }

            GameContextStep23TestDriver step23Driver = FindStep23Driver();
            if (step23Driver == null)
            {
                context.gameObject.AddComponent<GameContextStep23TestDriver>();
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

        private static GameContextHUD FindHUD()
        {
            GameContextHUD[] items = Resources.FindObjectsOfTypeAll<GameContextHUD>();
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

        private static GameContextStep19TestDriver FindStep19Driver()
        {
            GameContextStep19TestDriver[] items = Resources.FindObjectsOfTypeAll<GameContextStep19TestDriver>();
            if (items == null || items.Length == 0)
            {
                return null;
            }

            return items[0];
        }

        private static GameContextStep20TestDriver FindStep20Driver()
        {
            GameContextStep20TestDriver[] items = Resources.FindObjectsOfTypeAll<GameContextStep20TestDriver>();
            if (items == null || items.Length == 0)
            {
                return null;
            }

            return items[0];
        }

        private static GameContextStep21TestDriver FindStep21Driver()
        {
            GameContextStep21TestDriver[] items = Resources.FindObjectsOfTypeAll<GameContextStep21TestDriver>();
            if (items == null || items.Length == 0)
            {
                return null;
            }

            return items[0];
        }

        private static GameContextStep22TestDriver FindStep22Driver()
        {
            GameContextStep22TestDriver[] items = Resources.FindObjectsOfTypeAll<GameContextStep22TestDriver>();
            if (items == null || items.Length == 0)
            {
                return null;
            }

            return items[0];
        }

        private static GameContextStep23TestDriver FindStep23Driver()
        {
            GameContextStep23TestDriver[] items = Resources.FindObjectsOfTypeAll<GameContextStep23TestDriver>();
            if (items == null || items.Length == 0)
            {
                return null;
            }

            return items[0];
        }

    }
}
