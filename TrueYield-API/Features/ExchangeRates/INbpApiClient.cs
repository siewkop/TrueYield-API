namespace TrueYield_API.Features.ExchangeRates;

public interface INbpApiClient
{
    Task<decimal?> GetCurrencyRate(string code, DateOnly date, CancellationToken cancellationToken = default);
}
