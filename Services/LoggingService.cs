using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace RiotAutoLogin.Services
{
    public static class LoggingService
    {
        private static ILoggerFactory? _loggerFactory;
        private static string _logFilePath = string.Empty;

        public static void Initialize()
        {
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDirectory);
            
            _logFilePath = Path.Combine(logDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");
            
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });
        }

        public static ILogger<T> GetLogger<T>()
        {
            if (_loggerFactory == null)
            {
                Initialize();
            }
            return _loggerFactory!.CreateLogger<T>();
        }

        public static ILogger GetLogger(string categoryName)
        {
            if (_loggerFactory == null)
            {
                Initialize();
            }
            return _loggerFactory!.CreateLogger(categoryName);
        }

        public static void LogError(Exception ex, string message)
        {
            var logger = GetLogger("LoggingService");
            logger.LogError(ex, message);
        }

        public static void LogInformation(string message)
        {
            var logger = GetLogger("LoggingService");
            logger.LogInformation(message);
        }

        public static void Shutdown()
        {
            _loggerFactory?.Dispose();
            _loggerFactory = null;
        }
    }
} 