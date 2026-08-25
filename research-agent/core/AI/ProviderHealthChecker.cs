using System.Diagnostics;

namespace ResearchAgent.Core.AI;

public static class ProviderHealthChecker
{
    public static async Task<ProviderHealth> CheckAsync(IStructuredModelClient client, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await client.CompleteJsonAsync(
                "Return JSON only. Do not include prose.",
                "Return exactly this object: {\"status\":\"ok\"}", cancellationToken);
            sw.Stop();
            var available = response.Contains("\"status\"", StringComparison.OrdinalIgnoreCase) &&
                            response.Contains("ok", StringComparison.OrdinalIgnoreCase);
            return new(client.Provider, client.Model, available, sw.Elapsed,
                available ? null : "Health response did not contain expected status.", DateTimeOffset.UtcNow);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException)
        {
            sw.Stop();
            return new(client.Provider, client.Model, false, sw.Elapsed, ex.Message, DateTimeOffset.UtcNow);
        }
    }
}
