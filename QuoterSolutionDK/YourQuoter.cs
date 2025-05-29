using Microsoft.Extensions.Logging;
using QuoterSolutionDK.Contract;
using QuoterSolutionDK.Entity;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace QuoterSolutionDK
{

    public class YourQuoter : IQuoter
    {
        private readonly IMarketOrderSource _marketOrderSource;
        private readonly IQuoteStrategy _quoteStrategy;
        private readonly ILogger<YourQuoter> _logger;
        private readonly ConcurrentDictionary<string, List<MarketOrder>> _marketOrders = new();
        private readonly Channel<MarketOrder> _channel = Channel.CreateUnbounded<MarketOrder>();

        public YourQuoter(IMarketOrderSource source, IQuoteStrategy strategy, ILogger<YourQuoter> logger)
        {
            _marketOrderSource = source;
            _quoteStrategy = strategy;
            _logger = logger;

            _ = Task.Run(ProduceOrdersAsync);
            _ = Task.Run(ConsumeOrdersAsync);
           
        }

        private async Task ProduceOrdersAsync()
        {
            try
            {
                while (true)
                {
                    var order = await _marketOrderSource.GetNextMarketOrderAsync();
                    //throw new AggregateException();
                    await _channel.Writer.WriteAsync(order);
                    _logger.LogInformation("New market order received: {InstrumentId} @ {Price} x {Quantity}",
                        order.InstrumentId, order.Price, order.Quantity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProduceOrdersAsync");
            }

        }

        private async Task ConsumeOrdersAsync()
        {
            try
            {
                await foreach (var order in _channel.Reader.ReadAllAsync())
                {
                    var orders = _marketOrders.GetOrAdd(order.InstrumentId, _ => new List<MarketOrder>());
                    //throw new AggregateException();
                    lock (orders)
                    {
                        orders.Add(order);
                    }

                    _logger.LogInformation("***Order stored for {InstrumentId}. Total Orders: {Count}",
                        order.InstrumentId, orders.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConsumeOrdersAsync");
            }
        }

        public async Task<double> GetQuoteAsync(string instrumentId, int quantity)
        {
            try
            {
                await Task.Yield();

                if (!_marketOrders.TryGetValue(instrumentId, out var orders))
                {
                    _logger.LogWarning("No orders found for instrument: {InstrumentId}", instrumentId);
                    return -1;
                }

                List<MarketOrder> snapshot;
                lock (orders)
                {
                    snapshot = orders.ToList();
                }

                var quote = _quoteStrategy.GetQuote(snapshot, quantity);
                _logger.LogInformation("Quote calculated for {InstrumentId} @ Qty {Quantity} = {Quote}", instrumentId, quantity, quote);
                return quote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetQuoteAsync");
                return -1;
            }
        }

        public async Task<double> GetVolumeWeightedAveragePriceAsync(string instrumentId)
        {
            try
            {
                await Task.Yield();

                if (!_marketOrders.TryGetValue(instrumentId, out var orders))
                {
                    _logger.LogWarning("No orders found for instrument: {InstrumentId}", instrumentId);
                    return -1;
                }

                lock (orders)
                {
                    int totalQuantity = orders.Sum(o => o.Quantity);
                    if (totalQuantity == 0)
                    {
                        _logger.LogWarning("Total quantity is 0 for VWAP of {InstrumentId}", instrumentId);
                        return 0;
                    }

                    double weightedTotal = orders.Sum(o => o.Price * o.Quantity);
                    double vwap = weightedTotal / totalQuantity;
                    _logger.LogInformation("VWAP for {InstrumentId} is {VWAP}", instrumentId, vwap);
                    return vwap;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetVolumeWeightedAveragePriceAsync");
                return -1;
            }
        }
    }

}
