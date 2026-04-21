using System;
using System.Collections.Generic;

namespace OneManJourney.Runtime
{
    public sealed class GameEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out List<Delegate> delegates))
            {
                delegates = new List<Delegate>();
                _handlers[eventType] = delegates;
            }

            delegates.Add(handler);
            return new EventSubscription<TEvent>(this, handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                return;
            }

            Type eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out List<Delegate> delegates))
            {
                return;
            }

            delegates.Remove(handler);
            if (delegates.Count == 0)
            {
                _handlers.Remove(eventType);
            }
        }

        public void Publish<TEvent>(TEvent gameEvent)
        {
            Type eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out List<Delegate> delegates) || delegates.Count == 0)
            {
                return;
            }

            Delegate[] snapshot = delegates.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i] is Action<TEvent> callback)
                {
                    callback(gameEvent);
                }
            }
        }

        public void Clear()
        {
            _handlers.Clear();
        }

        private sealed class EventSubscription<TEvent> : IDisposable
        {
            private GameEventBus _eventBus;
            private Action<TEvent> _handler;
            private bool _isDisposed;

            public EventSubscription(GameEventBus eventBus, Action<TEvent> handler)
            {
                _eventBus = eventBus;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                _eventBus?.Unsubscribe(_handler);
                _eventBus = null;
                _handler = null;
                _isDisposed = true;
            }
        }
    }
}
