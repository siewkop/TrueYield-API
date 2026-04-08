namespace TrueYield_API.Features.Portfolio;

public interface IYieldCalculatorService
{
    PositionYieldDto CalculateYield(Position position, decimal currentStockPrice, decimal currentFxRate, string description);
}
