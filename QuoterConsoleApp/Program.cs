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
        quoter.Start();

        int qty = 120;
        string instrumentId = "DK50782120";

        Console.WriteLine("Waiting for market data...");

        // Wait until there is enough data to fulfill the quote
        while (true)
        {
            // Give some time for background tasks to process orders
            await Task.Delay(500);

            var quote = await quoter.GetQuoteAsync(instrumentId, qty);
            var vwap = await quoter.GetVolumeWeightedAveragePriceAsync(instrumentId);

            if (quote == 0)
            {
                Console.WriteLine("No orders found for instrument yet. Waiting...");
            }
            else if (quote < 0)
            {
                Console.WriteLine("Insufficient orders to fulfill the requested quantity. Waiting...");
            }
            else
            {
                Console.WriteLine($"\nQuote: {quote} (unit price: {quote / (double)qty})");
                Console.WriteLine($"VWAP : {vwap}");
                break;
            }
        }

        Console.WriteLine("Done.");
        quoter.Dispose();
    }
}
