namespace RiotAutoLogin.UI
{
    /// <summary>
    /// UI Constants to eliminate magic numbers and hard-coded values
    /// </summary>
    public static class UIConstants
    {
        // Button Sizes
        public const double COMPACT_BUTTON_WIDTH = 60.0;
        public const double COMPACT_BUTTON_HEIGHT = 28.0;
        public const double STANDARD_BUTTON_WIDTH = 100.0;
        public const double STANDARD_BUTTON_HEIGHT = 32.0;
        public const double SMALL_BUTTON_WIDTH = 45.0;
        public const double SMALL_BUTTON_HEIGHT = 20.0;
        public const double ICON_BUTTON_SIZE = 24.0;

        // Toggle Button Sizes
        public const double TOGGLE_BUTTON_WIDTH = 60.0;
        public const double TOGGLE_BUTTON_HEIGHT = 24.0;

        // Input Control Heights
        public const double INPUT_HEIGHT = 28.0;
        public const double LARGE_INPUT_HEIGHT = 32.0;

        // Card Sizes
        public const double LOGIN_CARD_WIDTH = 180.0;
        public const double LOGIN_CARD_HEIGHT = 260.0;
        public const double MANAGE_CARD_HEIGHT = 180.0;

        // Avatar Sizes
        public const double LOGIN_AVATAR_SIZE = 60.0;
        public const double MANAGE_AVATAR_SIZE = 35.0;

        // Rank Image Sizes
        public const double LOGIN_RANK_SIZE = 40.0;
        public const double MANAGE_RANK_SIZE = 18.0;

        // Margins
        public const double COMPACT_MARGIN = 8.0;
        public const double STANDARD_MARGIN = 12.0;
        public const double LARGE_MARGIN = 16.0;
        public const double SECTION_MARGIN = 20.0;

        // Font Sizes
        public const double SMALL_FONT_SIZE = 9.0;
        public const double COMPACT_FONT_SIZE = 11.0;
        public const double STANDARD_FONT_SIZE = 12.0;
        public const double LARGE_FONT_SIZE = 16.0;

        // Corner Radius
        public const double COMPACT_CORNER_RADIUS = 4.0;
        public const double STANDARD_CORNER_RADIUS = 8.0;
        public const double LARGE_CORNER_RADIUS = 12.0;
        public const double CIRCULAR_CORNER_RADIUS = 50.0; // Percentage

        // Loading Operation Names
        public static class Operations
        {
            // public const string VERIFY_ACCOUNTS = "VerifyAccounts"; // Removed verification constant
            public const string FETCH_RANKS = "FetchRanks";
            public const string FETCH_ALL_RANKS = "FetchAllRanks";
            public const string LOGIN = "Login";
            public const string ADD_ACCOUNT = "AddAccount";
            public const string UPDATE_ACCOUNT = "UpdateAccount";
            public const string DELETE_ACCOUNT = "DeleteAccount";
            public const string SAVE_SETTINGS = "SaveSettings";
        }

        // Animation Durations
        public const double FAST_ANIMATION_DURATION = 150.0;
        public const double STANDARD_ANIMATION_DURATION = 300.0;
        public const double SLOW_ANIMATION_DURATION = 500.0;
    }
} 