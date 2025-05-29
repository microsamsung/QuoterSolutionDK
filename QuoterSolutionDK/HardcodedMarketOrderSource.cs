using QuoterSolutionDK.Contract;
using QuoterSolutionDK.Entity;

namespace QuoterSolutionDK
{

    public class HardcodedMarketOrderSource : IMarketOrderSource
    {
        private int _position = 0;
        private readonly List<MarketOrder> _quotes = new()
    {
        new() { InstrumentId = "DK50782120", Price = 99.81, Quantity = 421 },
        new() { InstrumentId = "BA79603015", Price = 102.997, Quantity = 12 },
        new() { InstrumentId = "BA79603015", Price = 103.2, Quantity = 60 },
        new() { InstrumentId = "AB73567490", Price = 103.25, Quantity = 79 },
        new() { InstrumentId = "AB73567490", Price = 95.5, Quantity = 14 },
        new() { InstrumentId = "BA79603015", Price = 98.0, Quantity = 1 },
        new() { InstrumentId = "AB73567490", Price = 100.7, Quantity = 17 },
        new() { InstrumentId = "DK50782120", Price = 100.001, Quantity = 900 },
        new() { InstrumentId = "DK50782120", Price = 99.81, Quantity = 421 }
    };

        public async Task<MarketOrder> GetNextMarketOrderAsync()
        {
            if (_position >= _quotes.Count)
            {
                await Task.Delay(Timeout.Infinite);
            }

            await Task.Delay(500); // Simulate delay
            return _quotes[_position++];
        }
    }
}
