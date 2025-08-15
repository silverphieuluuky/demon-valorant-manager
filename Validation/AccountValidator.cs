using System;
using System.Text.RegularExpressions;
using RiotAutoLogin.Models;

namespace RiotAutoLogin.Validation
{
    public static class AccountValidator
    {
        private static readonly Regex GameNameRegex = new Regex(@"^[a-zA-Z0-9\s]{3,16}$", RegexOptions.Compiled);
        private static readonly Regex TagLineRegex = new Regex(@"^[a-zA-Z0-9]{3,5}$", RegexOptions.Compiled);
        private static readonly Regex UsernameRegex = new Regex(@"^[a-zA-Z0-9_]{3,20}$", RegexOptions.Compiled);
        private static readonly Regex PasswordRegex = new Regex(@"^.{6,}$", RegexOptions.Compiled);

        public static ValidationResult ValidateAccount(Account account)
        {
            var result = new ValidationResult();

            // Validate GameName
            if (string.IsNullOrWhiteSpace(account.GameName))
            {
                result.AddError(nameof(account.GameName), "Game name is required");
            }
            else if (!GameNameRegex.IsMatch(account.GameName))
            {
                result.AddError(nameof(account.GameName), "Game name must be 3-16 characters, letters, numbers, and spaces only");
            }

            // Validate TagLine
            if (string.IsNullOrWhiteSpace(account.TagLine))
            {
                result.AddError(nameof(account.TagLine), "Tag line is required");
            }
            else if (!TagLineRegex.IsMatch(account.TagLine))
            {
                result.AddError(nameof(account.TagLine), "Tag line must be 3-5 characters, letters and numbers only");
            }

            // Validate Username
            if (string.IsNullOrWhiteSpace(account.AccountName))
            {
                result.AddError(nameof(account.AccountName), "Username is required");
            }
            else if (!UsernameRegex.IsMatch(account.AccountName))
            {
                result.AddError(nameof(account.AccountName), "Username must be 3-20 characters, letters, numbers, and underscores only");
            }

            // Validate Region
            if (string.IsNullOrWhiteSpace(account.Region))
            {
                result.AddError(nameof(account.Region), "Region is required");
            }
            else if (!IsValidRegion(account.Region))
            {
                result.AddError(nameof(account.Region), "Invalid region. Must be one of: na, eu, ap, kr, br, latam");
            }

            return result;
        }

        public static ValidationResult ValidateAccountForAdd(string gameName, string tagLine, string username, string password, string region)
        {
            var result = new ValidationResult();

            // Validate GameName
            if (string.IsNullOrWhiteSpace(gameName))
            {
                result.AddError(nameof(gameName), "Game name is required");
            }
            else if (!GameNameRegex.IsMatch(gameName))
            {
                result.AddError(nameof(gameName), "Game name must be 3-16 characters, letters, numbers, and spaces only");
            }

            // Validate TagLine
            if (string.IsNullOrWhiteSpace(tagLine))
            {
                result.AddError(nameof(tagLine), "Tag line is required");
            }
            else if (!TagLineRegex.IsMatch(tagLine))
            {
                result.AddError(nameof(tagLine), "Tag line must be 3-5 characters, letters and numbers only");
            }

            // Validate Username
            if (string.IsNullOrWhiteSpace(username))
            {
                result.AddError(nameof(username), "Username is required");
            }
            else if (!UsernameRegex.IsMatch(username))
            {
                result.AddError(nameof(username), "Username must be 3-20 characters, letters, numbers, and underscores only");
            }

            // Validate Password
            if (string.IsNullOrWhiteSpace(password))
            {
                result.AddError(nameof(password), "Password is required");
            }
            else if (!PasswordRegex.IsMatch(password))
            {
                result.AddError(nameof(password), "Password must be at least 6 characters long");
            }

            // Validate Region
            if (string.IsNullOrWhiteSpace(region))
            {
                result.AddError(nameof(region), "Region is required");
            }
            else if (!IsValidRegion(region))
            {
                result.AddError(nameof(region), "Invalid region. Must be one of: na, eu, ap, kr, br, latam");
            }

            return result;
        }

        public static ValidationResult ValidateAccountForUpdate(string gameName, string tagLine, string username, string region)
        {
            var result = new ValidationResult();

            // Validate GameName
            if (string.IsNullOrWhiteSpace(gameName))
            {
                result.AddError(nameof(gameName), "Game name is required");
            }
            else if (!GameNameRegex.IsMatch(gameName))
            {
                result.AddError(nameof(gameName), "Game name must be 3-16 characters, letters, numbers, and spaces only");
            }

            // Validate TagLine
            if (string.IsNullOrWhiteSpace(tagLine))
            {
                result.AddError(nameof(tagLine), "Tag line is required");
            }
            else if (!TagLineRegex.IsMatch(tagLine))
            {
                result.AddError(nameof(tagLine), "Tag line must be 3-5 characters, letters and numbers only");
            }

            // Validate Username
            if (string.IsNullOrWhiteSpace(username))
            {
                result.AddError(nameof(username), "Username is required");
            }
            else if (!UsernameRegex.IsMatch(username))
            {
                result.AddError(nameof(username), "Username must be 3-20 characters, letters, numbers, and underscores only");
            }

            // Validate Region
            if (string.IsNullOrWhiteSpace(region))
            {
                result.AddError(nameof(region), "Region is required");
            }
            else if (!IsValidRegion(region))
            {
                result.AddError(nameof(region), "Invalid region. Must be one of: na, eu, ap, kr, br, latam");
            }

            return result;
        }

        private static bool IsValidRegion(string region)
        {
            var validRegions = new[] { "na", "eu", "ap", "kr", "br", "latam" };
            return Array.Exists(validRegions, r => r.Equals(region, StringComparison.OrdinalIgnoreCase));
        }
    }
} 