namespace TrueYield_API.Features.Portfolio;

public class YieldCalculatorService : IYieldCalculatorService
{
    public PositionYieldDto CalculateYield(Position position, decimal currentStockPrice, decimal currentFxRate, string description)
    {
        var totalPaidOriginal = position.Quantity * position.UnitPurchasePrice;
        var totalPaidPln = totalPaidOriginal * position.PurchaseExchangeRate;

        var currentValueOriginal = position.Quantity * currentStockPrice;
        var currentValuePln = currentValueOriginal * currentFxRate;

        var yieldOriginal = currentValueOriginal - totalPaidOriginal;
        var yieldPercentageOriginal = totalPaidOriginal > 0 ? (yieldOriginal / totalPaidOriginal) * 100 : 0;

        var yieldPln = currentValuePln - totalPaidPln;
        var yieldPercentagePln = totalPaidPln > 0 ? (yieldPln / totalPaidPln) * 100 : 0;

        return new PositionYieldDto(
            position.Symbol,
            description,
            position.Quantity,
            position.PurchaseCurrency,
            Math.Round(totalPaidOriginal, 2),
            Math.Round(currentValueOriginal, 2),
            Math.Round(yieldOriginal, 2),
            Math.Round(yieldPercentageOriginal, 2),
            Math.Round(totalPaidPln, 2),
            Math.Round(currentValuePln, 2),
            Math.Round(yieldPln, 2),
            Math.Round(yieldPercentagePln, 2)
        );
    }
}
