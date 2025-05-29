using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuoterSolutionDK;
using QuoterSolutionDK.Contract;

class Program
{
    static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(config => config.AddConsole())
            .AddSingleton<IMarketOrderSource, HardcodedMarketOrderSource>()
            .AddScoped<IQuoteStrategy, BestPriceStrategy>()
            .AddScoped<YourQuoter>()
            .BuildServiceProvider();

        var quoter = serviceProvider.GetRequiredService<YourQuoter>();
        //quoter.Start();
        int qty = 120;
        string instrumentId = "DK50782120";

        Console.WriteLine("Waiting for market data...");
        await Task.Delay(3000); // simulate waiting for data to populate

        var quote = await quoter.GetQuoteAsync(instrumentId, qty);
        var vwap = await quoter.GetVolumeWeightedAveragePriceAsync(instrumentId);

        Console.WriteLine($"\nQuote: {quote} (unit price: {quote / (double)qty})");
        Console.WriteLine($"VWAP : {vwap}");
        Console.WriteLine("Done.");
    }
}