using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace RiotAutoLogin.Services
{
    /// <summary>
    /// Manages loading states and provides user feedback during operations
    /// </summary>
    public class LoadingStateService
    {
        private readonly ILogger _logger;
        private readonly Dispatcher _dispatcher;
        private readonly Dictionary<string, bool> _loadingStates = new Dictionary<string, bool>();
        private readonly Dictionary<string, string> _loadingMessages = new Dictionary<string, string>();
        private readonly Dictionary<string, double> _progressValues = new Dictionary<string, double>();

        public event EventHandler<LoadingStateChangedEventArgs>? LoadingStateChanged;
        public event EventHandler<ProgressChangedEventArgs>? ProgressChanged;

        public LoadingStateService(ILogger logger, Dispatcher dispatcher)
        {
            _logger = logger;
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Starts a loading operation
        /// </summary>
        public void StartLoading(string operationId, string message = "Loading...")
        {
            try
            {
                _loadingStates[operationId] = true;
                _loadingMessages[operationId] = message;
                _progressValues[operationId] = 0.0;

                _logger.LogInformation($"Started loading operation: {operationId} - {message}");
                
                _dispatcher.Invoke(() =>
                {
                    LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs(operationId, true, message));
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error starting loading operation: {operationId}");
            }
        }

        /// <summary>
        /// Stops a loading operation
        /// </summary>
        public void StopLoading(string operationId, string finalMessage = "Completed")
        {
            try
            {
                if (_loadingStates.ContainsKey(operationId))
                {
                    _loadingStates[operationId] = false;
                    _loadingMessages[operationId] = finalMessage;
                    _progressValues[operationId] = 100.0;

                    _logger.LogInformation($"Stopped loading operation: {operationId} - {finalMessage}");
                    
                    _dispatcher.Invoke(() =>
                    {
                        LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs(operationId, false, finalMessage));
                        ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(operationId, 100.0));
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error stopping loading operation: {operationId}");
            }
        }

        /// <summary>
        /// Updates progress for a loading operation
        /// </summary>
        public void UpdateProgress(string operationId, double progress, string? message = null)
        {
            try
            {
                if (_loadingStates.ContainsKey(operationId) && _loadingStates[operationId])
                {
                    _progressValues[operationId] = Math.Max(0, Math.Min(100, progress));
                    
                    if (!string.IsNullOrEmpty(message))
                    {
                        _loadingMessages[operationId] = message;
                    }

                    _logger.LogDebug($"Updated progress for {operationId}: {progress:F1}% - {message}");
                    
                    _dispatcher.Invoke(() =>
                    {
                        ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(operationId, _progressValues[operationId]));
                        if (!string.IsNullOrEmpty(message))
                        {
                            LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs(operationId, true, message));
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating progress for operation: {operationId}");
            }
        }

        /// <summary>
        /// Checks if an operation is currently loading
        /// </summary>
        public bool IsLoading(string operationId)
        {
            return _loadingStates.ContainsKey(operationId) && _loadingStates[operationId];
        }

        /// <summary>
        /// Gets the current loading message for an operation
        /// </summary>
        public string GetLoadingMessage(string operationId)
        {
            return _loadingMessages.ContainsKey(operationId) ? _loadingMessages[operationId] : string.Empty;
        }

        /// <summary>
        /// Gets the current progress for an operation
        /// </summary>
        public double GetProgress(string operationId)
        {
            return _progressValues.ContainsKey(operationId) ? _progressValues[operationId] : 0.0;
        }

        /// <summary>
        /// Wraps an async operation with loading state management
        /// </summary>
        public async Task<T> ExecuteWithLoadingAsync<T>(string operationId, string loadingMessage, Func<Task<T>> operation)
        {
            try
            {
                StartLoading(operationId, loadingMessage);
                
                var result = await operation();
                
                StopLoading(operationId, "Completed successfully");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in loading operation: {operationId}");
                StopLoading(operationId, $"Failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Wraps an async operation with loading state management and progress updates
        /// </summary>
        public async Task<T> ExecuteWithLoadingAndProgressAsync<T>(string operationId, string loadingMessage, 
            Func<IProgress<double>, Task<T>> operation)
        {
            try
            {
                StartLoading(operationId, loadingMessage);
                
                var progress = new Progress<double>(progressValue =>
                {
                    UpdateProgress(operationId, progressValue);
                });
                
                var result = await operation(progress);
                
                StopLoading(operationId, "Completed successfully");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in loading operation with progress: {operationId}");
                StopLoading(operationId, $"Failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Shows a temporary loading message
        /// </summary>
        public async Task ShowTemporaryMessageAsync(string operationId, string message, int durationMs = 2000)
        {
            try
            {
                StartLoading(operationId, message);
                await Task.Delay(durationMs);
                StopLoading(operationId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing temporary message: {operationId}");
            }
        }

        /// <summary>
        /// Clears all loading states
        /// </summary>
        public void ClearAllLoadingStates()
        {
            try
            {
                var operationIds = new List<string>(_loadingStates.Keys);
                foreach (var operationId in operationIds)
                {
                    StopLoading(operationId, "Cleared");
                }
                
                _loadingStates.Clear();
                _loadingMessages.Clear();
                _progressValues.Clear();
                
                _logger.LogInformation("Cleared all loading states");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing loading states");
            }
        }
    }

    /// <summary>
    /// Event arguments for loading state changes
    /// </summary>
    public class LoadingStateChangedEventArgs : EventArgs
    {
        public string OperationId { get; }
        public bool IsLoading { get; }
        public string Message { get; }

        public LoadingStateChangedEventArgs(string operationId, bool isLoading, string message)
        {
            OperationId = operationId;
            IsLoading = isLoading;
            Message = message;
        }
    }

    /// <summary>
    /// Event arguments for progress changes
    /// </summary>
    public class ProgressChangedEventArgs : EventArgs
    {
        public string OperationId { get; }
        public double Progress { get; }

        public ProgressChangedEventArgs(string operationId, double progress)
        {
            OperationId = operationId;
            Progress = progress;
        }
    }
} 