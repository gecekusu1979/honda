// ServiceContainer.cs — Aşama 2: Backward-Compat DI Köprüsü
//
// Bu sınıf artık gerçek Microsoft.Extensions.DependencyInjection container'ını
// kullanır. Mevcut ServiceContainer.Resolve<T>() çağrıları değiştirilmeden derlenir.
//
// Yeni kod için: IServiceProvider'ı constructor injection ile kullanın.
// Eski kod için: ServiceContainer.Resolve<T>() backward-compat olarak korunmuştur.
//
// Mock/test override kayıtları için ServiceContainer.Register<T>() hâlâ kullanılabilir
// (bu durumda statik override dictionary önceliklidir).

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace HondaTuner.Core.Container
{
    /// <summary>
    /// Backward-compat DI köprüsü.
    /// Dahili olarak Microsoft.Extensions.DependencyInjection IServiceProvider kullanır.
    /// Mevcut Resolve{T}() ve Register{T}() API'si korunmuştur.
    /// </summary>
    public static class ServiceContainer
    {
        // Test/mock override dictionary — DI container'dan önce kontrol edilir
        private static readonly Dictionary<Type, object> _overrides = new Dictionary<Type, object>();

        // Gerçek DI provider — lazy oluşturulur
        private static IServiceProvider _provider;
        private static readonly object _lock = new object();

        /// <summary>
        /// IServiceProvider'ı alır ya da oluşturur. İlk çağrıda AppServiceCollection kullanılır.
        /// </summary>
        private static IServiceProvider Provider
        {
            get
            {
                if (_provider == null)
                {
                    lock (_lock)
                    {
                        if (_provider == null)
                        {
                            var services = new ServiceCollection();
                            AppServiceCollection.ConfigureServices(services);
                            _provider = services.BuildServiceProvider();
                        }
                    }
                }
                return _provider;
            }
        }

        /// <summary>
        /// Test veya runtime override için bir servis örneğini kaydeder.
        /// Build edilmiş provider'dan önce kontrol edilir.
        /// </summary>
        public static void Register<T>(T serviceInstance)
        {
            if (serviceInstance == null) throw new ArgumentNullException(nameof(serviceInstance));
            lock (_overrides)
            {
                _overrides[typeof(T)] = serviceInstance;
            }
        }

        /// <summary>
        /// Servisi çözümler. Override varsa onu döner; yoksa IServiceProvider'ı kullanır.
        /// </summary>
        public static T Resolve<T>() where T : class
        {
            var type = typeof(T);
            lock (_overrides)
            {
                if (_overrides.TryGetValue(type, out var overrideInstance))
                    return (T)overrideInstance;
            }
            return Provider.GetRequiredService<T>();
        }

        /// <summary>
        /// Override kayıtlarını sıfırlar (test isolation için kullanılır).
        /// DI provider sıfırlanmaz — sadece manuel overrides temizlenir.
        /// </summary>
        public static void ResetOverrides()
        {
            lock (_overrides)
            {
                _overrides.Clear();
            }
        }

        /// <summary>
        /// [Deprecated] Backward-compat alias. ResetOverrides() tercih edilir.
        /// DI provider cache'ini de yeniden oluşturmak için kullanın.
        /// </summary>
        public static void Reset()
        {
            lock (_overrides)
            {
                _overrides.Clear();
            }
            lock (_lock)
            {
                (_provider as IDisposable)?.Dispose();
                _provider = null;
            }
        }

        /// <summary>
        /// Dışarıdan önceden oluşturulmuş bir IServiceProvider'ı enjekte eder.
        /// Program.cs'ten IHost kullanıldığında çağrılır.
        /// </summary>
        public static void UseProvider(IServiceProvider provider)
        {
            lock (_lock)
            {
                _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            }
        }
    }
}
