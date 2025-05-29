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
        }

        [TestMethod]
        public async Task GetQuoteAsync_ReturnsExpectedQuote()
        {
            await Task.Delay(1000); // wait for background to load
            double quote = await _quoter.GetQuoteAsync("BA79603015", 30);
            Assert.IsTrue(quote > 0);
        }

        [TestMethod]
        public async Task GetQuoteAsync_ReturnsMinusOne_WhenInstrumentNotFound()
        {
            double quote = await _quoter.GetQuoteAsync("SA79603015", 10);
            Assert.AreEqual(-1, quote);
        }

        [TestMethod]
        public async Task GetQuoteAsync_ReturnsMinusOne_WhenInsufficientQuantity()
        {
            await Task.Delay(1000);
            double quote = await _quoter.GetQuoteAsync("BA79603015", 900); // only 100 exists
            Assert.AreEqual(-1, quote);
        }

        [TestMethod]
        public async Task GetVolumeWeightedAveragePriceAsync_ReturnsExpectedVWAP()
        {
            await Task.Delay(1000);
            double vwap = await _quoter.GetVolumeWeightedAveragePriceAsync("BA79603015");
            Assert.IsTrue(vwap > 0);
        }

        [TestMethod]
        public async Task GetVolumeWeightedAveragePriceAsync_ReturnsMinusOne_WhenInstrumentNotFound()
        {
            double vwap = await _quoter.GetVolumeWeightedAveragePriceAsync("SA79603015");
            Assert.AreEqual(-1, vwap);
        }

        [TestMethod]
        public async Task GetVolumeWeightedAveragePriceAsync_ReturnsZero_WhenQuantityZero()
        {
            var orderSource = new EmptyMarketOrderSource();
            var quoter = new YourQuoter(orderSource, _strategy!, _loggerMock!.Object);
            await Task.Delay(1000);
            double vwap = await quoter.GetVolumeWeightedAveragePriceAsync("EMPTY007");
            Assert.AreEqual(0, vwap);
        }

        // TestMarketOrderSource to simulate real orders
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
                await Task.Delay(200);
                if (_index < _orders.Count)
                    return _orders[_index++];
                await Task.Delay(Timeout.Infinite); // simulate infinite wait
                return null;
            }
        }

        // Empty source with 0 quantity
        private class EmptyMarketOrderSource : IMarketOrderSource
        {
            public async Task<MarketOrder> GetNextMarketOrderAsync()
            {
                await Task.Delay(200);
                return new MarketOrder { InstrumentId = "EMPTY007", Price = 100.0, Quantity = 0 };
            }
        }
    }
}