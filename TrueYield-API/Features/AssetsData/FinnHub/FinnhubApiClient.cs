using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TrueYield_API.Features.AssetsData.FinnHub;

internal record FinnhubAssetDto(
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("displaySymbol")] string DisplaySymbol,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("type")] string Type
);

internal record FinnhubPriceDto(
    [property: JsonPropertyName("c")] decimal CurrentPrice
);

public class FinnhubApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<FinnhubApiClient> logger) : IAssetsDataProvider
{
    private const string BaseUrl = "https://finnhub.io/api/v1";

    public async Task<Dictionary<string, AssetModel>> GetAvailableAssets(CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["Finnhub:ApiKey"];
        var url = $"{BaseUrl}/stock/symbol?exchange=US&token={apiKey}";

        try
        {
            var finnhubAssets = await httpClient.GetFromJsonAsync<List<FinnhubAssetDto>>(url, cancellationToken);
            if (finnhubAssets is null) return new Dictionary<string, AssetModel>();

            return finnhubAssets
                .Where(x => !string.IsNullOrWhiteSpace(x.Symbol))
                .GroupBy(x => x.Symbol)
                .Select(g => g.First())
                .ToDictionary(
                    x => x.Symbol,
                    x => new AssetModel(x.Symbol, x.Currency, x.Description, x.Type)
                );
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Invalid Finnhub API Key. Please update your configuration.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch Finnhub assets");
            throw;
        }
    }

    public async Task<Dictionary<string, AssetPrice>> GetLatestPrice(List<string> symbols, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["Finnhub:ApiKey"];
        var results = new ConcurrentBag<AssetPrice>();

        var options = new ParallelOptions 
        { 
            MaxDegreeOfParallelism = 5, 
            CancellationToken = cancellationToken 
        };

        await Parallel.ForEachAsync(symbols, options, async (symbol, token) =>
        {
            var url = $"{BaseUrl}/quote?symbol={symbol}&token={apiKey}";
            try
            {
                var result = await httpClient.GetFromJsonAsync<FinnhubPriceDto>(url, token);
                if (result != null && result.CurrentPrice > 0)
                {
                    results.Add(new AssetPrice(symbol, result.CurrentPrice, null));
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException("Invalid Finnhub API Key. Please update your configuration.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Skiping symbol {Symbol} due to error (e.g. 404 or 429)", symbol);
            }
        });

        return results.ToDictionary(x => x.Symbol, x => x);
    }

    public Task<Dictionary<string, AssetPrice>> GetEndOfDayPrice(List<string> symbols, DateOnly date, CancellationToken cancellationToken = default)
    {
        return Task.FromException<Dictionary<string, AssetPrice>>(new NotImplementedException());
    }
}
