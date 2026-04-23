using System;
using System.Collections.Generic;

namespace OneManJourney.Runtime
{
    public static class GameServices
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Services[typeof(T)] = service;
        }

        public static void Unregister<T>() where T : class
        {
            Services.Remove(typeof(T));
        }

        public static bool TryResolve<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object boxedService) && boxedService is T typedService)
            {
                service = typedService;
                return true;
            }

            service = null;
            return false;
        }

        public static T Resolve<T>() where T : class
        {
            if (TryResolve(out T service))
            {
                return service;
            }

            throw new InvalidOperationException($"Service not found: {typeof(T).Name}");
        }
    }
}
