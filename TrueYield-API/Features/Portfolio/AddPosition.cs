using TrueYield_API.Database;
using TrueYield_API.Features.ExchangeRates;

namespace TrueYield_API.Features.Portfolio;

// DTO for incoming request payload
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
        // 1. Manual Validation (as requested, instead of FluentValidation)
        if (request.Quantity <= 0)
        {
            return Results.BadRequest(new { Error = "Quantity must be greater than zero." });
        }

        if (request.UnitPurchasePrice <= 0)
        {
            return Results.BadRequest(new { Error = "Purchase price must be greater than zero." });
        }

        if (request.PurchaseDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Results.BadRequest(new { Error = "Purchase date cannot be in the future." });
        }
        
        if (string.IsNullOrWhiteSpace(request.Symbol) || string.IsNullOrWhiteSpace(request.PurchaseCurrency))
        {
            return Results.BadRequest(new { Error = "Symbol and Currency must be provided." });
        }

        // 2. Fetch the historical exchange rate representing the true value at the time of purchase
        decimal? exchangeRate = await nbpApiClient.GetCurrencyRate(request.PurchaseCurrency, request.PurchaseDate, cancellationToken);
        
        if (exchangeRate == null)
        {
            return Results.BadRequest(new { Error = $"Could not obtain exchange rate from NBP for '{request.PurchaseCurrency}' on {request.PurchaseDate}. Note: NBP does not track all currencies on all days."});
        }

        // 3. Create the Domain Entity
        var position = new Position
        {
            Id = Guid.NewGuid(),
            // Capitalize symbol to ensure cleanliness in DB
            Symbol = request.Symbol.ToUpperInvariant(),
            Quantity = request.Quantity,
            UnitPurchasePrice = request.UnitPurchasePrice,
            PurchaseCurrency = request.PurchaseCurrency.ToUpperInvariant(),
            PurchaseDate = request.PurchaseDate,
            PurchaseExchangeRate = exchangeRate.Value
        };

        // 4. Save to Database
        dbContext.Positions.Add(position);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 5. Return success
        return Results.Ok(new { Message = "Position successfully added to portfolio.", PositionId = position.Id });
    }
}
