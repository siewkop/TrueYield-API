using TrueYield_API.Database;
using TrueYield_API.Features.ExchangeRates;

namespace TrueYield_API.Features.Portfolio;

public record AddPositionRequest(
    string Symbol,
    decimal Quantity,
    decimal UnitPurchasePrice,
    string PurchaseCurrency,
    DateOnly PurchaseDate);

public static class AddPosition
{
    public static async Task<IResult> Handle(
        AddPositionRequest request,
        AppDbContext dbContext,
        INbpApiClient nbpApiClient,
        CancellationToken cancellationToken)
    {
        var validationError = GetValidationError(request);
        if (validationError != null)
        {
            return Results.BadRequest(new { Error = validationError });
        }

        var exchangeRate = await nbpApiClient.GetCurrencyRate(request.PurchaseCurrency, request.PurchaseDate, cancellationToken);
        if (exchangeRate == null)
        {
            return Results.BadRequest(new { Error = $"Could not obtain exchange rate from NBP for '{request.PurchaseCurrency}' on {request.PurchaseDate}. Note: NBP does not track all currencies on all days."});
        }

        var position = new Position
        {
            Id = Guid.NewGuid(),
            Symbol = request.Symbol.Trim().ToUpperInvariant(),
            Quantity = request.Quantity,
            UnitPurchasePrice = request.UnitPurchasePrice,
            PurchaseCurrency = request.PurchaseCurrency.Trim().ToUpperInvariant(),
            PurchaseDate = request.PurchaseDate,
            PurchaseExchangeRate = exchangeRate.Value
        };

        dbContext.Positions.Add(position);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { Message = "Position successfully added to portfolio.", PositionId = position.Id });
    }

    private static string? GetValidationError(AddPositionRequest request)
    {
        if (request.Quantity <= 0) return "Quantity must be greater than zero.";
        if (request.UnitPurchasePrice <= 0) return "Purchase price must be greater than zero.";
        if (request.PurchaseDate > DateOnly.FromDateTime(DateTime.UtcNow)) return "Purchase date cannot be in the future.";
        if (string.IsNullOrWhiteSpace(request.Symbol) || string.IsNullOrWhiteSpace(request.PurchaseCurrency)) return "Symbol and Currency must be provided.";
        
        return null;
    }
}
