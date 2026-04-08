namespace TrueYield_API.Features.AssetsData
{
    public interface IAssetsDataProvider
    {
        Task<Dictionary<string, AssetModel>> GetAvailableAssets(CancellationToken cancellationToken = default);

        Task<Dictionary<string, AssetPrice>> GetEndOfDayPrice(List<string> symbols, DateOnly date, CancellationToken cancellationToken = default);

        Task<Dictionary<string, AssetPrice>> GetLatestPrice(List<string> symbols, CancellationToken cancellationToken = default);
    }
}
