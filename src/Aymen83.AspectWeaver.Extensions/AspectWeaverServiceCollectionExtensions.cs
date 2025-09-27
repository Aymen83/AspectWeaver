using Aymen83.AspectWeaver.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Aymen83.AspectWeaver.Extensions
{
    /// <summary>
    /// Extension methods for setting up AspectWeaver handlers in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class AspectWeaverServiceCollectionExtensions
    {
        /// <summary>
        /// Scans the specified assembly for concrete implementations of <see cref="IAspectHandler{TAttribute}"/>
        /// and registers them with the specified <see cref="ServiceLifetime"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="assembly">The assembly to scan for handlers.</param>
        /// <param name="lifetime">The lifecycle for the registered handlers (default is Scoped).</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddAspectWeaverHandlers(
            this IServiceCollection services,
            Assembly assembly,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var openGenericInterface = typeof(IAspectHandler<>);

            var typesToRegister = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
                .Select(implementationType => new
                {
                    ImplementationType = implementationType,
                    ServiceInterface = implementationType.GetInterfaces()
                        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface)
                })
                .Where(x => x.ServiceInterface != null);

            foreach (var registration in typesToRegister)
            {
                var descriptor = new ServiceDescriptor(
                    registration.ServiceInterface!,
                    registration.ImplementationType,
                    lifetime);

                services.Add(descriptor);
            }

            return services;
        }

        /// <summary>
        /// Scans the assembly containing the specified type <typeparamref name="TMarker"/> for aspect handlers
        /// and registers them with the specified <see cref="ServiceLifetime"/>.
        /// </summary>
        /// <typeparam name="TMarker">A type located in the assembly to scan (e.g., the Startup or Program class).</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="lifetime">The lifecycle for the registered handlers (default is Scoped).</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddAspectWeaverHandlers<TMarker>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return AddAspectWeaverHandlers(services, typeof(TMarker).Assembly, lifetime);
        }
    }
}