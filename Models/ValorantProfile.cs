using System;

namespace RiotAutoLogin.Models
{
    public class ValorantProfile
    {
        public string Username { get; set; } = "";
        public string Tag { get; set; } = "";
        public string Region { get; set; } = "";
        public string CurrentRank { get; set; } = "";
        public string PeakRank { get; set; } = "";
        public int RankRating { get; set; } = 0;
        public DateTime LastUpdated { get; set; }
        public string FullUsername => $"{Username}#{Tag}";
        public string DisplayRank => string.IsNullOrEmpty(CurrentRank) ? "Unranked" : CurrentRank;
        public string DisplayPeakRank => string.IsNullOrEmpty(PeakRank) ? "No Peak" : PeakRank;
    }
}
