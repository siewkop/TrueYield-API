using Microsoft.EntityFrameworkCore;
using TrueYield_API.Database;
using TrueYield_API.Features.AssetsData;
using TrueYield_API.Features.AssetsData.FinnHub;
using TrueYield_API.Features.ExchangeRates;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<INbpApiClient, NbpApiClient>()
    .AddStandardResilienceHandler();

builder.Services.AddHttpClient<IAssetsDataProvider, FinnhubApiClient>()
    .AddStandardResilienceHandler();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/test-nbp", async (INbpApiClient nbpClient) =>
{
    var testDate = new DateOnly(2026, 4, 5);

    var rate = await nbpClient.GetCurrencyRate("USD",testDate);

    if (rate is not null)
    {
        return Results.Ok(new
        {
            Message = "Success!",
            Date = testDate,
            UsdToPlnRate = rate
        });
    }

    return Results.NotFound("Rate not found. It might be a weekend or holiday!");
});

app.Run();
