using System.Text;
using System.Net.Http.Json;

namespace TrueYield_API.Features.ExchangeRates
{
    public class NbpApiClient(HttpClient httpClient, ILogger<NbpApiClient> logger) : INbpApiClient
    {
        private const string BaseUrl = "https://api.nbp.pl/api/exchangerates/rates/a";
        private const int MaxFallbackDays = 5;

        public async Task<decimal?> GetCurrencyRate(string code, DateOnly date)
        {
            for (int i = 0; i <= MaxFallbackDays; i++)
            {
                var targetDate = date.AddDays(-i);
                var dateString = targetDate.ToString("yyyy-MM-dd");
                var url = $"{BaseUrl}/{code}/{dateString}/?format=json";

                try
                {
                    if (i > 0)
                    {
                        logger.LogInformation("Attempting fallback NBP rate for {Code} on {Date} (original date: {OriginalDate})", code, targetDate, date);
                    }

                    var response = await httpClient.GetFromJsonAsync<NbpResponseDto>(url);
                    
                    if (response?.Rates != null && response.Rates.Count > 0)
                    {
                        return response.Rates.First().Mid;
                    }
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    logger.LogWarning("NBP rate not found for {Code} on {Date}", code, targetDate);
                }
            }

            logger.LogError("Failed to find NBP rate for {Code} around {Date} after {FallbackDays} fallback days.", code, date, MaxFallbackDays);
            return null;
        }
    }
}
