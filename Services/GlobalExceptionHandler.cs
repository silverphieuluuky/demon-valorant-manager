using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace RiotAutoLogin.Services
{
    /// <summary>
    /// Global exception handler for the application
    /// </summary>
    public class GlobalExceptionHandler
    {
        private readonly ILogger _logger;
        private readonly Dispatcher _dispatcher;

        public GlobalExceptionHandler(ILogger logger, Dispatcher dispatcher)
        {
            _logger = logger;
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Handles unhandled exceptions globally
        /// </summary>
        public void HandleException(Exception exception, string context = "Unknown")
        {
            try
            {
                _logger.LogError(exception, $"Unhandled exception in context: {context}");
                
                // Log additional context information
                LogExceptionContext(exception, context);
                
                // Show user-friendly error message
                ShowUserFriendlyError(exception, context);
            }
            catch (Exception ex)
            {
                // Fallback error handling
                _logger.LogError(ex, "Error in global exception handler");
                MessageBox.Show("A critical error occurred. Please restart the application.", 
                    "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles exceptions asynchronously
        /// </summary>
        public async Task HandleExceptionAsync(Exception exception, string context = "Unknown")
        {
            try
            {
                _logger.LogError(exception, $"Async exception in context: {context}");
                
                await Task.Run(() => LogExceptionContext(exception, context));
                
                await _dispatcher.InvokeAsync(() => ShowUserFriendlyError(exception, context));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in async global exception handler");
                await _dispatcher.InvokeAsync(() => 
                    MessageBox.Show("A critical error occurred. Please restart the application.", 
                        "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        /// <summary>
        /// Logs additional context information for the exception
        /// </summary>
        private void LogExceptionContext(Exception exception, string context)
        {
            try
            {
                var contextInfo = new
                {
                    Context = context,
                    Timestamp = DateTime.Now,
                    ExceptionType = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    InnerException = exception.InnerException?.Message
                };

                _logger.LogError("Exception context: {@ContextInfo}", contextInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging exception context");
            }
        }

        /// <summary>
        /// Shows a user-friendly error message
        /// </summary>
        private void ShowUserFriendlyError(Exception exception, string context)
        {
            try
            {
                string userMessage = GetUserFriendlyMessage(exception, context);
                string title = GetErrorTitle(exception);
                MessageBoxImage icon = GetErrorIcon(exception);

                MessageBox.Show(userMessage, title, MessageBoxButton.OK, icon);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing user-friendly error message");
                MessageBox.Show("An unexpected error occurred. Please try again.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Gets a user-friendly error message based on the exception type
        /// </summary>
        private string GetUserFriendlyMessage(Exception exception, string context)
        {
            return exception switch
            {
                UnauthorizedAccessException => "Access denied. Please check your permissions and try again.",
                System.IO.IOException => "File operation failed. Please check if the file is in use or if you have sufficient permissions.",
                System.Net.WebException => "Network connection failed. Please check your internet connection and try again.",
                System.Threading.Tasks.TaskCanceledException => "Operation was cancelled. Please try again.",
                ArgumentException => "Invalid input provided. Please check your data and try again.",
                InvalidOperationException => "Operation cannot be performed at this time. Please try again later.",
                _ => $"An error occurred while {context.ToLower()}. Please try again or contact support if the problem persists."
            };
        }

        /// <summary>
        /// Gets an appropriate error title based on the exception type
        /// </summary>
        private string GetErrorTitle(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => "Access Denied",
                System.IO.IOException => "File Error",
                System.Net.WebException => "Network Error",
                System.Threading.Tasks.TaskCanceledException => "Operation Cancelled",
                ArgumentException => "Invalid Input",
                InvalidOperationException => "Operation Failed",
                _ => "Error"
            };
        }

        /// <summary>
        /// Gets an appropriate error icon based on the exception type
        /// </summary>
        private MessageBoxImage GetErrorIcon(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => MessageBoxImage.Warning,
                System.IO.IOException => MessageBoxImage.Warning,
                System.Net.WebException => MessageBoxImage.Warning,
                System.Threading.Tasks.TaskCanceledException => MessageBoxImage.Information,
                ArgumentException => MessageBoxImage.Warning,
                InvalidOperationException => MessageBoxImage.Warning,
                _ => MessageBoxImage.Error
            };
        }

        /// <summary>
        /// Sets up global exception handling for the application
        /// </summary>
        public void SetupGlobalExceptionHandling()
        {
            try
            {
                // Handle unhandled exceptions in the current domain
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    if (e.ExceptionObject is Exception exception)
                    {
                        HandleException(exception, "AppDomain.UnhandledException");
                    }
                };

                // Handle unhandled exceptions in the dispatcher
                _dispatcher.UnhandledException += (sender, e) =>
                {
                    HandleException(e.Exception, "Dispatcher.UnhandledException");
                    e.Handled = true; // Mark as handled to prevent application crash
                };

                // Handle unhandled exceptions in tasks
                TaskScheduler.UnobservedTaskException += (sender, e) =>
                {
                    HandleException(e.Exception, "TaskScheduler.UnobservedTaskException");
                    e.SetObserved(); // Mark as observed to prevent application crash
                };

                _logger.LogInformation("Global exception handling setup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up global exception handling");
            }
        }

        /// <summary>
        /// Wraps an action with exception handling
        /// </summary>
        public void ExecuteWithExceptionHandling(Action action, string context = "Unknown")
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                HandleException(ex, context);
            }
        }

        /// <summary>
        /// Wraps an async action with exception handling
        /// </summary>
        public async Task ExecuteWithExceptionHandlingAsync(Func<Task> action, string context = "Unknown")
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, context);
            }
        }

        /// <summary>
        /// Wraps a function with exception handling and returns a default value on error
        /// </summary>
        public T ExecuteWithExceptionHandling<T>(Func<T> func, T defaultValue, string context = "Unknown")
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                HandleException(ex, context);
                return defaultValue;
            }
        }

        /// <summary>
        /// Wraps an async function with exception handling and returns a default value on error
        /// </summary>
        public async Task<T> ExecuteWithExceptionHandlingAsync<T>(Func<Task<T>> func, T defaultValue, string context = "Unknown")
        {
            try
            {
                return await func();
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, context);
                return defaultValue;
            }
        }
    }
} 