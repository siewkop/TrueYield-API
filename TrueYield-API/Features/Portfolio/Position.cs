namespace TrueYield_API.Features.Portfolio;

public class Position
{
    public Guid Id { get; set; }
    
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    
    public decimal UnitPurchasePrice { get; set; }
    public string PurchaseCurrency { get; set; } = "USD";
    public DateOnly PurchaseDate { get; set; }
    
    public decimal PurchaseExchangeRate { get; set; }
}
