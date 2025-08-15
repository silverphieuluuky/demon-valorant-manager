using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using RiotAutoLogin.Models;

namespace RiotAutoLogin.Services
{
    public static class RankFetchLogger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RiotClientAutoLogin", "Logs");

        private static readonly string RankFetchLogFile = Path.Combine(LogDirectory, "rank_fetch_errors.txt");
        private static readonly ILogger _logger = LoggingService.GetLogger("RankFetchLogger");

        static RankFetchLogger()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating log directory");
            }
        }

        public static void LogRankFetchStart(string operation, int accountCount)
        {
            var logEntry = new StringBuilder();
            logEntry.AppendLine("=".PadRight(80, '='));
            logEntry.AppendLine($"RANK FETCH OPERATION STARTED");
            logEntry.AppendLine($"Operation: {operation}");
            logEntry.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logEntry.AppendLine($"Account Count: {accountCount}");
            logEntry.AppendLine($"Default Server: {ServerSettingsService.GetDefaultServer()}");
            logEntry.AppendLine($"API Key Configured: {!string.IsNullOrEmpty(ApiKeyManager.GetHenrikDevApiKey())}");
            logEntry.AppendLine("=".PadRight(80, '='));

            WriteToFile(logEntry.ToString());
        }

        public static void LogRankFetchSuccess(Account account, string rank, int mmr, string server)
        {
            var logEntry = new StringBuilder();
            logEntry.AppendLine($"[SUCCESS] {DateTime.Now:HH:mm:ss} - {account.GameName}#{account.TagLine}");
            logEntry.AppendLine($"  Server: {server}");
            logEntry.AppendLine($"  Rank: {rank}");
            logEntry.AppendLine($"  MMR: {mmr}");
            logEntry.AppendLine($"  IsValidRank: {account.HasValidRank}");

            WriteToFile(logEntry.ToString());
        }

        public static void LogRankFetchError(Account account, string error, string server, Exception? ex = null)
        {
            var logEntry = new StringBuilder();
            logEntry.AppendLine($"[ERROR] {DateTime.Now:HH:mm:ss} - {account.GameName}#{account.TagLine}");
            logEntry.AppendLine($"  Server: {server}");
            logEntry.AppendLine($"  Error: {error}");
            
            if (ex != null)
            {
                logEntry.AppendLine($"  Exception Type: {ex.GetType().Name}");
                logEntry.AppendLine($"  Exception Message: {ex.Message}");
                logEntry.AppendLine($"  Stack Trace: {ex.StackTrace}");
            }

            WriteToFile(logEntry.ToString());
        }

        public static void LogRankFetchNoData(Account account, string server, string reason)
        {
            var logEntry = new StringBuilder();
            logEntry.AppendLine($"[NO DATA] {DateTime.Now:HH:mm:ss} - {account.GameName}#{account.TagLine}");
            logEntry.AppendLine($"  Server: {server}");
            logEntry.AppendLine($"  Reason: {reason}");

            WriteToFile(logEntry.ToString());
        }

        public static void LogRankFetchSummary(int totalAccounts, int successfulFetches, int failedFetches, int noDataFetches)
        {
            var logEntry = new StringBuilder();
            logEntry.AppendLine("-".PadRight(80, '-'));
            logEntry.AppendLine($"RANK FETCH SUMMARY - {DateTime.Now:HH:mm:ss}");
            logEntry.AppendLine($"Total Accounts: {totalAccounts}");
            logEntry.AppendLine($"Successful: {successfulFetches}");
            logEntry.AppendLine($"Failed: {failedFetches}");
            logEntry.AppendLine($"No Data: {noDataFetches}");
            logEntry.AppendLine($"Success Rate: {(totalAccounts > 0 ? (successfulFetches * 100.0 / totalAccounts) : 0):F1}%");
            logEntry.AppendLine("-".PadRight(80, '-'));
            logEntry.AppendLine(); // Empty line for separation

            WriteToFile(logEntry.ToString());
        }

        public static void LogApiResponse(string accountName, string response, bool isSuccess)
        {
            var logEntry = new StringBuilder();
            logEntry.AppendLine($"[API RESPONSE] {DateTime.Now:HH:mm:ss} - {accountName}");
            logEntry.AppendLine($"  Success: {isSuccess}");
            logEntry.AppendLine($"  Response: {response}");
            logEntry.AppendLine();

            WriteToFile(logEntry.ToString());
        }

        public static void LogRateLimitInfo(string accountName, int retryCount, int delayMs)
        {
            var logEntry = new StringBuilder();
            logEntry.AppendLine($"[RATE LIMIT] {DateTime.Now:HH:mm:ss} - {accountName}");
            logEntry.AppendLine($"  Retry Count: {retryCount}");
            logEntry.AppendLine($"  Delay: {delayMs}ms");
            logEntry.AppendLine();

            WriteToFile(logEntry.ToString());
        }

        private static void WriteToFile(string content)
        {
            try
            {
                File.AppendAllText(RankFetchLogFile, content, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to rank fetch log file");
            }
        }

        public static string GetLogFilePath()
        {
            return RankFetchLogFile;
        }

        public static void ClearLogFile()
        {
            try
            {
                if (File.Exists(RankFetchLogFile))
                {
                    File.Delete(RankFetchLogFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing rank fetch log file");
            }
        }

        public static string GetLogContent()
        {
            try
            {
                if (File.Exists(RankFetchLogFile))
                {
                    return File.ReadAllText(RankFetchLogFile, Encoding.UTF8);
                }
                return "Log file not found.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading rank fetch log file");
                return $"Error reading log file: {ex.Message}";
            }
        }
    }
}
