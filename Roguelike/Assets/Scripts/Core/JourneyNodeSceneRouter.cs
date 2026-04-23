using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneManJourney.Runtime
{
    [DisallowMultipleComponent]
    public sealed class JourneyNodeSceneRouter : MonoBehaviour
    {
        [SerializeField] private bool _autoLoadOnNodeEntered = true;

        private GameContext _context;
        private GameEventBus _eventBus;
        private IDisposable _journeyNodeEnteredSubscription;

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
            TryBind();
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

            if (_eventBus == null)
            {
                return false;
            }

            if (_journeyNodeEnteredSubscription == null)
            {
                _journeyNodeEnteredSubscription = _eventBus.Subscribe<JourneyNodeEnteredEvent>(HandleJourneyNodeEntered);
            }

            return true;
        }

        private void Unbind()
        {
            _journeyNodeEnteredSubscription?.Dispose();
            _journeyNodeEnteredSubscription = null;
            _eventBus = null;
            _context = null;
        }

        private void HandleJourneyNodeEntered(JourneyNodeEnteredEvent evt)
        {
            if (!_autoLoadOnNodeEntered)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(evt.SceneName))
            {
                return;
            }

            if (!IsSceneInBuildSettings(evt.SceneName))
            {
                Debug.LogWarning($"JourneyNodeSceneRouter: scene '{evt.SceneName}' is not in Build Settings.");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (string.Equals(activeScene.name, evt.SceneName, StringComparison.Ordinal))
            {
                return;
            }

            SceneManager.LoadScene(evt.SceneName, LoadSceneMode.Single);
        }

        private static bool IsSceneInBuildSettings(string sceneName)
        {
            int sceneCount = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string candidateName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (string.Equals(candidateName, sceneName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
