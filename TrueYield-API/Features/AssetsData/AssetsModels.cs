namespace TrueYield_API.Features.AssetsData
{
    public record AssetModel(string Symbol, string Currency, string Description, string Type);
    
    public record AssetPrice(string Symbol, decimal Price, string? Currency);
}
