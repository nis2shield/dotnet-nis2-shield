using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nis2Shield.AspNetCore.Configuration;

namespace Nis2Shield.AspNetCore.ActiveDefense;

/// <summary>
/// Detects and blocks requests from Tor exit nodes.
/// Uses a cached list of known exit nodes.
/// </summary>
public class TorBlocker
{
    private readonly Nis2Options _options;
    private readonly ILogger<TorBlocker> _logger;
    private readonly HashSet<string> _torExitNodes = new();
    private readonly SemaphoreSlim _updateLock = new(1, 1);
    private DateTime _lastUpdate = DateTime.MinValue;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromHours(6);

    public TorBlocker(IOptions<Nis2Options> options, ILogger<TorBlocker> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Checks if the given IP is a known Tor exit node.
    /// </summary>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <returns>True if it's a Tor exit node, false otherwise.</returns>
    public async Task<bool> IsTorExitNodeAsync(string? ipAddress)
    {
        if (!_options.ActiveDefense.BlockTorExitNodes || string.IsNullOrEmpty(ipAddress))
            return false;

        // Update list if needed
        if (DateTime.UtcNow - _lastUpdate > UpdateInterval)
        {
            await UpdateTorListAsync();
        }

        var isBlocked = _torExitNodes.Contains(ipAddress);
        
        if (isBlocked)
        {
            _logger.LogWarning("Blocked Tor exit node: {IpAddress}", ipAddress);
        }

        return isBlocked;
    }

    /// <summary>
    /// Manually updates the Tor exit node list.
    /// </summary>
    public async Task UpdateTorListAsync()
    {
        if (!await _updateLock.WaitAsync(0))
            return; // Another update is in progress

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await client.GetStringAsync("https://check.torproject.org/torbulkexitlist");
            
            var nodes = response
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.StartsWith('#'))
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line));

            _torExitNodes.Clear();
            foreach (var node in nodes)
            {
                _torExitNodes.Add(node);
            }

            _lastUpdate = DateTime.UtcNow;
            _logger.LogInformation("Updated Tor exit node list: {Count} nodes", _torExitNodes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Tor exit node list");
        }
        finally
        {
            _updateLock.Release();
        }
    }
}
