using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using TrueYield_API.Features.ExchangeRates;
using Xunit;

namespace TrueYield_API_Tests.Features.ExchangeRates;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 2. We execute the custom logic passed into the constructor to determine what to respond with
        return Task.FromResult(_responseFactory(request));
    }
}

public class NbpApiClientTests
{
    [Fact]
    public async Task GetCurrencyRate_WhenRateExistsOnGivenDate_ReturnsMidValue()
    {
        // Arrange
        var testDate = new DateOnly(2023, 10, 12);
        var expectedRate = 4.32m;

        // 3. We configure our Fake Handler to respond with our fake JSON
        var fakeHandler = new FakeHttpMessageHandler(request =>
        {
            if (request.RequestUri?.ToString() == "https://api.nbp.pl/api/exchangerates/rates/a/USD/2023-10-12/?format=json")
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("""
                    {
                        "table": "A",
                        "currency": "dolar amerykański",
                        "code": "USD",
                        "rates": [
                            {
                                "no": "197/A/NBP/2023",
                                "effectiveDate": "2023-10-12",
                                "mid": 4.32
                            }
                        ]
                    }
                    """)
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var httpClient = new HttpClient(fakeHandler);
        var logger = NullLogger<NbpApiClient>.Instance; // Vanilla way to provide a blank logger in tests
        var sut = new NbpApiClient(httpClient, logger); // sut = System Under Test

        // Act
        var result = await sut.GetCurrencyRate("USD", testDate);

        // Assert (using Vanilla xUnit Assert)
        Assert.Equal(expectedRate, result);
    }

    [Fact]
    public async Task GetCurrencyRate_WhenGivenDateIsWeekend_LoopsBackAndReturnsPrecedingRate()
    {
        // Arrange
        var sundayDate = new DateOnly(2023, 10, 15);

        // We configure our Fake Handler to return 404 for Sunday and Saturday, 
        // but return a successful JSON for Friday.
        var fakeHandler = new FakeHttpMessageHandler(request =>
        {
            var url = request.RequestUri?.ToString();

            if (url == "https://api.nbp.pl/api/exchangerates/rates/a/USD/2023-10-13/?format=json")
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("""{ "rates": [ { "mid": 4.25 } ] }""")
                };
            }

            // For any other URL (like Sunday or Saturday), return 404
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var httpClient = new HttpClient(fakeHandler);
        var sut = new NbpApiClient(httpClient, NullLogger<NbpApiClient>.Instance);

        // Act
        var result = await sut.GetCurrencyRate("USD", sundayDate);

        // Assert
        Assert.Equal(4.25m, result);
    }
}
