using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TrueYield_API.Database;
using TrueYield_API.Features.AssetsData;
using TrueYield_API.Features.ExchangeRates;

namespace TrueYield_API.Features.Portfolio;

public record PositionYieldDto(
    string Symbol,
    string Description,
    decimal Quantity,
    string PurchaseCurrency,
    decimal TotalPaidOriginal,
    decimal CurrentValueOriginal,
    decimal YieldOriginal,
    decimal YieldPercentageOriginal,
    decimal TotalPaidPln,
    decimal CurrentValuePln,
    decimal YieldPln,
    decimal YieldPercentagePln);

public record PortfolioSummaryResult(
    decimal TotalPortfolioValuePln,
    decimal TotalPortfolioPaidPln,
    decimal TotalPortfolioYieldPln,
    decimal TotalPortfolioYieldPercentage,
    List<PositionYieldDto> Positions);

public static class GetPortfolioSnapshot
{
    public static async Task<IResult> Handle(
        AppDbContext dbContext,
        IAssetsDataProvider finnhub,
        INbpApiClient nbp,
        IMemoryCache cache,
        IYieldCalculatorService yieldCalculator,
        CancellationToken cancellationToken)
    {
        var positions = await dbContext.Positions.ToListAsync(cancellationToken);
        
        if (!positions.Any())
        {
            return Results.Ok(new PortfolioSummaryResult(0, 0, 0, 0, new List<PositionYieldDto>()));
        }

        var distinctSymbols = positions.Select(p => p.Symbol).Distinct().ToList();
        var currentPricesTask = finnhub.GetLatestPrice(distinctSymbols, cancellationToken);
        
        if (!cache.TryGetValue("finnhub_assets", out Dictionary<string, AssetModel>? assetMetadata))
        {
            assetMetadata = await finnhub.GetAvailableAssets(cancellationToken);
            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(24));
            cache.Set("finnhub_assets", assetMetadata, cacheOptions);
        }

        var distinctCurrencies = positions.Select(p => p.PurchaseCurrency).Distinct().ToList();
        var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
        
        var currentFxRates = new Dictionary<string, decimal>();
        foreach (var currency in distinctCurrencies)
        {
            if (currency.Equals("PLN", StringComparison.InvariantCultureIgnoreCase))
            {
                currentFxRates["PLN"] = 1.0m;
                continue;
            }

            var rate = await nbp.GetCurrencyRate(currency, currentDate, cancellationToken);
            currentFxRates[currency.ToUpperInvariant()] = rate ?? 1.0m;
        }

        var currentPrices = await currentPricesTask;

        var yieldDtos = new List<PositionYieldDto>();

        foreach (var pos in positions)
        {
            var description = assetMetadata?.TryGetValue(pos.Symbol, out var meta) == true ? meta.Description : "Unknown Asset";
            var currentStockPrice = currentPrices.TryGetValue(pos.Symbol, out var priceObj) ? priceObj.Price : pos.UnitPurchasePrice;
            var currentFxRate = currentFxRates.GetValueOrDefault(pos.PurchaseCurrency, 1.0m);

            var yieldDto = yieldCalculator.CalculateYield(pos, currentStockPrice, currentFxRate, description);
            yieldDtos.Add(yieldDto);
        }

        var totalPortfolioPaidPln = yieldDtos.Sum(x => x.TotalPaidPln);
        var totalPortfolioValuePln = yieldDtos.Sum(x => x.CurrentValuePln);
        var totalPortfolioYieldPln = totalPortfolioValuePln - totalPortfolioPaidPln;
        var totalPortfolioYieldPercentage = totalPortfolioPaidPln > 0 
            ? (totalPortfolioYieldPln / totalPortfolioPaidPln) * 100 
            : 0;

        var result = new PortfolioSummaryResult(
            Math.Round(totalPortfolioValuePln, 2),
            Math.Round(totalPortfolioPaidPln, 2),
            Math.Round(totalPortfolioYieldPln, 2),
            Math.Round(totalPortfolioYieldPercentage, 2),
            yieldDtos.OrderByDescending(x => x.CurrentValuePln).ToList()
        );

        return Results.Ok(result);
    }
}
