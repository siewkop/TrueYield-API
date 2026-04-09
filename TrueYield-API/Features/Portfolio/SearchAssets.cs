using Microsoft.Extensions.Caching.Memory;
using TrueYield_API.Features.AssetsData;

namespace TrueYield_API.Features.Portfolio;

public static class SearchAssets
{
    public static async Task<IResult> Handle(
        string q,
        IAssetsDataProvider finnhub,
        IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Results.Ok(new List<AssetModel>());
        }

        try
        {
            if (!cache.TryGetValue("finnhub_assets", out Dictionary<string, AssetModel>? assetMetadata))
            {
                assetMetadata = await finnhub.GetAvailableAssets(cancellationToken);
                var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(24));
                cache.Set("finnhub_assets", assetMetadata, cacheOptions);
            }

            if (assetMetadata == null)
            {
                return Results.Ok(new List<AssetModel>());
            }

            var query = q.Trim().ToUpperInvariant();

            var matches = assetMetadata.Values
                .Where(a => a.Symbol.StartsWith(query) || (a.Description != null && a.Description.Contains(query, StringComparison.InvariantCultureIgnoreCase)))
                .OrderBy(a => a.Symbol.Length)
                .ThenBy(a => a.Symbol)
                .Take(20)
                .ToList();

            return Results.Ok(matches);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Finnhub"))
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
