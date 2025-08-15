using System;
using System.Windows;
using RiotAutoLogin.Services;
using RiotAutoLogin.Interfaces;
using RiotAutoLogin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RiotAutoLogin
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Initialize logging service first
                LoggingService.Initialize();
                LoggingService.LogInformation("Application starting...");

                // Setup dependency injection
                SetupDependencyInjection();

                base.OnStartup(e);
                
                LoggingService.LogInformation("Application started successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Failed to start application");
                throw;
            }
        }

        private void SetupDependencyInjection()
        {
            var services = new ServiceCollection();

            // Register logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            // Register Memory Cache
            services.AddMemoryCache();
            
            // Register services with factory methods to handle constructor dependencies
            services.AddSingleton<IConfigurationService>(provider => 
                new OptimizedConfigurationService(provider.GetRequiredService<ILogger<OptimizedConfigurationService>>()));
            
            services.AddSingleton<IHenrikDevService>(provider => 
                new HenrikDevService());
            
            services.AddSingleton<IRiotClientAutomationService>(provider => 
                new RiotClientAutomationService(provider.GetRequiredService<ILogger<RiotClientAutomationService>>()));
            
            services.AddSingleton<IAccountService>(provider => 
                new OptimizedAccountService(
                    provider.GetRequiredService<ILogger<OptimizedAccountService>>()));
            
            // Register Cache Service
            services.AddSingleton<AccountCacheService>();

            // Register ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<ManageViewModel>();
            services.AddTransient<SettingsViewModel>();

            _serviceProvider = services.BuildServiceProvider();
            
            LoggingService.LogInformation("Dependency injection setup completed");
        }

        public static T GetService<T>() where T : class
        {
            if (Current is App app && app._serviceProvider != null)
            {
                return app._serviceProvider.GetRequiredService<T>();
            }
            throw new InvalidOperationException("Service provider not available");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                LoggingService.LogInformation("Application shutting down...");
                
                // Dispose service provider
                _serviceProvider?.Dispose();
                
                // Cleanup services
                LoggingService.Shutdown();
                
                base.OnExit(e);
                
                LoggingService.LogInformation("Application shutdown completed");
            }
            catch (Exception)
            {
                // Silent fail during shutdown - logging service may not be available
            }
        }
    }
}
