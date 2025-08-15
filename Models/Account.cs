using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RiotAutoLogin.Models
{
    public class Account : INotifyPropertyChanged
    {
        private string _gameName = string.Empty;
        private string _tagLine = string.Empty;
        private string _accountName = string.Empty;
        private string _encryptedPassword = string.Empty;
        private string _region = "NA";
        private string _avatarPath = string.Empty;
        private string _currentRank = string.Empty;
        private string _peakRank = string.Empty;
        private int _rankRating = 0;
        private DateTime _lastRankUpdate = DateTime.MinValue;
        private string _lastError = string.Empty;
        private bool _isRankLoading = false;

        public string GameName
        {
            get => _gameName;
            set => SetProperty(ref _gameName, value);
        }

        public string TagLine
        {
            get => _tagLine;
            set => SetProperty(ref _tagLine, value);
        }

        public string AccountName
        {
            get => _accountName;
            set => SetProperty(ref _accountName, value);
        }

        public string EncryptedPassword
        {
            get => _encryptedPassword;
            set => SetProperty(ref _encryptedPassword, value);
        }

        public string Region
        {
            get => _region;
            set => SetProperty(ref _region, value);
        }

        public string AvatarPath
        {
            get => _avatarPath;
            set => SetProperty(ref _avatarPath, value);
        }

        public string CurrentRank
        {
            get => _currentRank;
            set => SetProperty(ref _currentRank, value);
        }

        public string PeakRank
        {
            get => _peakRank;
            set => SetProperty(ref _peakRank, value);
        }

        public int RankRating
        {
            get => _rankRating;
            set => SetProperty(ref _rankRating, value);
        }

        public DateTime LastRankUpdate
        {
            get => _lastRankUpdate;
            set => SetProperty(ref _lastRankUpdate, value);
        }

        public string LastError
        {
            get => _lastError;
            set => SetProperty(ref _lastError, value);
        }

        public bool IsRankLoading
        {
            get => _isRankLoading;
            set => SetProperty(ref _isRankLoading, value);
        }

        private bool _isRankLoaded = false;
        private bool _isRankFailed = false;

        public bool IsRankLoaded
        {
            get => _isRankLoaded;
            set => SetProperty(ref _isRankLoaded, value);
        }

        public bool IsRankFailed
        {
            get => _isRankFailed;
            set => SetProperty(ref _isRankFailed, value);
        }

        public string DisplayRank => string.IsNullOrEmpty(CurrentRank) ? "Unranked" : CurrentRank;
        public string DisplayPeakRank => string.IsNullOrEmpty(PeakRank) ? "No Peak" : PeakRank;
        public string FullUsername => $"{GameName}#{TagLine}";
        public bool HasError => !string.IsNullOrEmpty(LastError);
        
        // Display region name instead of region code
        public string DisplayRegion
        {
            get
            {
                return Region switch
                {
                    "AP" => "Asia Pacific (AP)",
                    "NA" => "North America (NA)",
                    "EU" => "Europe (EU)",
                    "KR" => "Korea (KR)",
                    "BR" => "Brazil (BR)",
                    "LATAM" => "Latin America (LATAM)",
                    _ => Region
                };
            }
        }
        
        // Computed property to determine if account has a valid rank
        public bool HasValidRank
        {
            get
            {
                if (string.IsNullOrEmpty(CurrentRank)) return false;
                var rank = CurrentRank.Trim().ToLower();
                return rank != "unrated" && rank != "unranked" && rank != "unknown";
            }
        }
        
        // Computed property to determine if rank should be displayed
        public bool ShouldDisplayRank => !IsRankLoading && !IsRankFailed;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
