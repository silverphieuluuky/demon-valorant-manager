using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using RiotAutoLogin.Models;
using Microsoft.Extensions.Logging;
using RiotAutoLogin.Interfaces;

namespace RiotAutoLogin.Services
{
    public class HenrikDevService : IHenrikDevService
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly Random _random = new Random();
        private readonly ILogger _logger = LoggingService.GetLogger<HenrikDevService>();

        public async Task<ValorantProfile?> GetPlayerProfileAsync(string username, string tag, string region, string apiKey, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var result = await TryGetPlayerProfileAsync(username, tag, region, apiKey);
                    if (result != null) return result;
                    
                    if (attempt < maxRetries)
                    {
                        var delay = Math.Pow(2, attempt) * 1000; // Exponential backoff
                        _logger.LogDebug($"Retry attempt {attempt}/{maxRetries} for {username}#{tag} in {delay}ms");
                        await Task.Delay((int)delay);
                    }
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning($"Rate limit exceeded for {username}#{tag}, attempt {attempt}/{maxRetries}");
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(60000); // Wait 1 minute for rate limit
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error getting profile for {username}#{tag}, attempt {attempt}/{maxRetries}");
                    if (attempt == maxRetries) break;
                }
            }
            return null;
        }

        private async Task<ValorantProfile?> TryGetPlayerProfileAsync(string username, string tag, string region, string apiKey)
        {
            try
            {
                // Wait 2–3 seconds to avoid rate-limiting
                await Task.Delay(_random.Next(2000, 3000));
                
                var mappedRegion = MapRegionToHenrikDev(region);
                var url = $"https://api.henrikdev.xyz/valorant/v2/mmr/{mappedRegion}/{Uri.EscapeDataString(username)}/{Uri.EscapeDataString(tag)}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", apiKey);
                
                _logger.LogDebug($"Making HenrikDev API request: {url.Replace(apiKey, "***")}");
                
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // Parse error response according to HenrikDev API documentation
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(content);
                        var errorRoot = errorDoc.RootElement;
                        
                        if (errorRoot.TryGetProperty("status", out var status) && 
                            errorRoot.TryGetProperty("errors", out var errors))
                        {
                            var statusCode = status.GetInt32();
                            var errorMessage = errors[0].GetProperty("message").GetString() ?? "Unknown error";
                            
                            _logger.LogWarning($"HenrikDev API error {statusCode}: {errorMessage} for {username}#{tag}");
                            
                            if (statusCode == 429)
                            {
                                throw new HttpRequestException("Rate Limited", null, System.Net.HttpStatusCode.TooManyRequests);
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        _logger.LogWarning($"Non-JSON error response from HenrikDev API: {content}");
                    }
                    return null;
                }

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("data", out var data) && 
                    data.TryGetProperty("current_data", out var currentData))
                {
                    var rank = currentData.TryGetProperty("currenttierpatched", out var rankProp) 
                        ? rankProp.GetString() ?? "Unknown" : "Unknown";
                    int mmr = currentData.TryGetProperty("elo", out var eloProp) 
                        ? eloProp.GetInt32() : 0;
                    var peakRank = currentData.TryGetProperty("highest_rank", out var peakRankProp) 
                        ? peakRankProp.GetString() ?? "No Peak" : "No Peak";
                    
                    _logger.LogDebug($"Successfully retrieved rank data for {username}#{tag}: {rank} (MMR: {mmr})");
                    
                    return new ValorantProfile
                    {
                        Username = username,
                        Tag = tag,
                        Region = region,
                        CurrentRank = rank,
                        RankRating = mmr,
                        PeakRank = peakRank,
                        LastUpdated = DateTime.UtcNow
                    };
                }
                
                _logger.LogWarning($"No rank data found in response for {username}#{tag}");
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP error getting profile for {username}#{tag}");
                throw; // Re-throw to be handled by retry logic
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"JSON parsing error for {username}#{tag}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error getting profile for {username}#{tag}");
                return null;
            }
        }

        private static string MapRegionToHenrikDev(string region)
        {
            return region.ToUpper() switch
            {
                "NA" => "na",
                "EU" => "eu", 
                "AP" => "ap",
                "KR" => "kr",
                "BR" => "br",
                "LATAM" => "latam",
                _ => "ap" // Default to AP as per HenrikDev documentation
            };
        }
    }


}
