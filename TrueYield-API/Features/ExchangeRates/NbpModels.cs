namespace TrueYield_API.Features.ExchangeRates
{
    public record NbpRateDto(string EffectiveDate, decimal Mid);
    public record NbpResponseDto(string Code, List<NbpRateDto> Rates);


}
