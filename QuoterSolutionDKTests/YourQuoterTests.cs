using Microsoft.Extensions.Logging;
using Moq;
using QuoterSolutionDK.Contract;
using QuoterSolutionDK.Entity;

namespace QuoterSolutionDK.Tests
{
    [TestClass]
    public class YourQuoterTests
    {
        private YourQuoter? _quoter;
        private Mock<ILogger<YourQuoter>>? _loggerMock;
        private IMarketOrderSource? _orderSource;
        private IQuoteStrategy? _strategy;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<YourQuoter>>();
            _orderSource = new MockMarketOrderSource();
            _strategy = new BestPriceStrategy();
            _quoter = new YourQuoter(_orderSource, _strategy, _loggerMock.Object);
            _quoter.Start();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _quoter?.Dispose();
        }

        [TestMethod]
        public async Task GetQuoteAsync_ReturnsExpectedQuote()
        {
            await Task.Delay(500); // Give time for orders to be consumed

            double quote = await _quoter!.GetQuoteAsync("BA79603015", 30);
            Assert.IsTrue(quote > 0);
        }

        [TestMethod]
        public async Task GetQuoteAsync_ReturnsZero_WhenInstrumentNotFound()
        {
            double quote = await _quoter!.GetQuoteAsync("UNKNOWN", 10);
            Assert.AreEqual(0, quote);
        }

        [TestMethod]
        public async Task GetQuoteAsync_Throws_WhenInsufficientQuantity()
        {
            await Task.Delay(500); // Give time for orders to be consumed

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            {
                await _quoter!.GetQuoteAsync("BA79603015", 900); // Only 100 available
            });
        }

        [TestMethod]
        public async Task GetVolumeWeightedAveragePriceAsync_ReturnsExpectedVWAP()
        {
            await Task.Delay(500);

            double vwap = await _quoter!.GetVolumeWeightedAveragePriceAsync("BA79603015");
            Assert.IsTrue(vwap > 0);
        }

        [TestMethod]
        public async Task GetVolumeWeightedAveragePriceAsync_ReturnsZero_WhenInstrumentNotFound()
        {
            double vwap = await _quoter!.GetVolumeWeightedAveragePriceAsync("UNKNOWN");
            Assert.AreEqual(0, vwap);
        }

        [TestMethod]
        public async Task GetVolumeWeightedAveragePriceAsync_ReturnsZero_WhenQuantityZero()
        {
            var orderSource = new EmptyMarketOrderSource();
            var quoter = new YourQuoter(orderSource, _strategy!, _loggerMock!.Object);
            quoter.Start();

            await Task.Delay(500);
            double vwap = await quoter.GetVolumeWeightedAveragePriceAsync("EMPTY007");
            Assert.AreEqual(0, vwap);

            quoter.Dispose();
        }

        // Mock data sources

        private class MockMarketOrderSource : IMarketOrderSource
        {
            private int _index = 0;
            private readonly List<MarketOrder> _orders = new()
            {
                new() { InstrumentId = "BA79603015", Price = 100.0, Quantity = 50 },
                new() { InstrumentId = "BA79603015", Price = 105.0, Quantity = 50 }
            };

            public async Task<MarketOrder> GetNextMarketOrderAsync()
            {
                await Task.Delay(100);
                if (_index < _orders.Count)
                    return _orders[_index++];
                await Task.Delay(Timeout.Infinite); // Simulate endless source
                throw new InvalidOperationException("Should not reach here!");
            }
        }

        private class EmptyMarketOrderSource : IMarketOrderSource
        {
            public async Task<MarketOrder> GetNextMarketOrderAsync()
            {
                await Task.Delay(100);
                return new MarketOrder { InstrumentId = "EMPTY007", Price = 100.0, Quantity = 0 };
            }
        }
    }
}
