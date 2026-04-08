using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TrueYield_API.Features.AssetsData;
using TrueYield_API.Features.AssetsData.FinnHub;
using Xunit;

namespace TrueYield_API_Tests.Features.AssetsData;

public class FinnhubFakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public FinnhubFakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_responseFactory(request));
    }
}

public class FinnhubApiClientTests
{
    private readonly IConfiguration _configuration;

    public FinnhubApiClientTests()
    {
        var inMemorySettings = new Dictionary<string, string?> {
            {"Finnhub:ApiKey", "FAKE_API_KEY"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task GetAvailableAssets_ReturnsMappedDictionary()
    {
        var fakeHandler = new FinnhubFakeHttpMessageHandler(request =>
        {
            if (request.RequestUri?.ToString().Contains("/stock/symbol") == true)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("""
                    [
                        {
                            "currency": "USD",
                            "description": "APPLE INC",
                            "displaySymbol": "AAPL",
                            "symbol": "AAPL",
                            "type": "Common Stock"
                        }
                    ]
                    """)
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var httpClient = new HttpClient(fakeHandler);
        var sut = new FinnhubApiClient(httpClient, _configuration, NullLogger<FinnhubApiClient>.Instance);

        var result = await sut.GetAvailableAssets();

        Assert.NotNull(result);
        Assert.True(result.ContainsKey("AAPL"));
        Assert.Equal("APPLE INC", result["AAPL"].Description);
        Assert.Equal("USD", result["AAPL"].Currency);
    }

    [Fact]
    public async Task GetLatestPrice_WithMultipleSymbols_HandlesPartialErrorsAndReturnsSuccessful()
    {
        var symbols = new List<string> { "AAPL", "MSFT", "BROKEN" };

        var fakeHandler = new FinnhubFakeHttpMessageHandler(request =>
        {
            var url = request.RequestUri?.ToString();
            
            if (url != null && url.Contains("symbol=AAPL"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent("""{ "c": 150.50 }""")
                };
            }
            if (url != null && url.Contains("symbol=MSFT"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent("""{ "c": 310.20 }""")
                };
            }
            
            return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        });

        var httpClient = new HttpClient(fakeHandler);
        var sut = new FinnhubApiClient(httpClient, _configuration, NullLogger<FinnhubApiClient>.Instance);

        var result = await sut.GetLatestPrice(symbols);

        Assert.Equal(2, result.Count);
        Assert.Equal(150.50m, result["AAPL"].Price);
        Assert.Equal(310.20m, result["MSFT"].Price);
    }
}
