using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RiotAutoLogin.ViewModels;

namespace RiotAutoLogin.ViewModels
{
    /// <summary>
    /// Base ViewModel class that provides loading state management to eliminate code duplication
    /// </summary>
    public abstract class BaseViewModelWithLoading : BaseViewModel
    {
        private readonly Dictionary<string, bool> _loadingStates = new();
        private readonly List<ICommand> _managedCommands = new();

        protected BaseViewModelWithLoading(ILogger logger) : base(logger)
        {
        }

        /// <summary>
        /// Registers a command to be automatically disabled during any loading operation
        /// </summary>
        protected void RegisterCommand(ICommand command)
        {
            if (command != null && !_managedCommands.Contains(command))
            {
                _managedCommands.Add(command);
            }
        }

        /// <summary>
        /// Sets the loading state for a specific operation
        /// </summary>
        protected void SetLoadingState(string operation, bool isLoading)
        {
            _loadingStates[operation] = isLoading;
            OnPropertyChanged(nameof(IsAnyLoading));
            RaiseAllCommandsCanExecuteChanged();
        }

        /// <summary>
        /// Gets the loading state for a specific operation
        /// </summary>
        protected bool IsLoading(string operation) => _loadingStates.GetValueOrDefault(operation, false);

        /// <summary>
        /// Returns true if any operation is currently loading
        /// </summary>
        public bool IsAnyLoading => _loadingStates.Values.Any(x => x);

        /// <summary>
        /// Returns true if the specified operation is not loading
        /// </summary>
        protected bool CanExecute(string operation) => !IsLoading(operation);

        /// <summary>
        /// Returns true if no operations are loading
        /// </summary>
        protected bool CanExecuteAny() => !IsAnyLoading;

        /// <summary>
        /// Raises CanExecuteChanged for all registered commands
        /// </summary>
        private void RaiseAllCommandsCanExecuteChanged()
        {
            foreach (var command in _managedCommands)
            {
                if (command is AsyncRelayCommand asyncCommand)
                {
                    asyncCommand.RaiseCanExecuteChanged();
                }
                else if (command is RelayCommand relayCommand)
                {
                    // RelayCommand doesn't have RaiseCanExecuteChanged, so we'll just trigger property change
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Clears all loading states
        /// </summary>
        protected void ClearAllLoadingStates()
        {
            _loadingStates.Clear();
            OnPropertyChanged(nameof(IsAnyLoading));
            RaiseAllCommandsCanExecuteChanged();
        }
    }
} 