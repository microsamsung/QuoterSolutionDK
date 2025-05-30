using Microsoft.Extensions.Logging;
using QuoterSolutionDK.Contract;
using QuoterSolutionDK.Entity;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace QuoterSolutionDK
{
    public class YourQuoter : IQuoter, IDisposable
    {
        private readonly IMarketOrderSource _marketOrderSource;
        private readonly IQuoteStrategy _quoteStrategy;
        private readonly ILogger<YourQuoter> _logger;
        private readonly ConcurrentDictionary<string, List<MarketOrder>> _marketOrders = new();
        private readonly Channel<MarketOrder> _channel = Channel.CreateUnbounded<MarketOrder>();
        private readonly CancellationTokenSource _cts = new();

        private Task? _produceTask;
        private Task? _consumeTask;

        public YourQuoter(IMarketOrderSource source, IQuoteStrategy strategy, ILogger<YourQuoter> logger)
        {
            _marketOrderSource = source ?? throw new ArgumentNullException(nameof(source));
            _quoteStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            //_ = Task.Run(() => ProduceOrdersAsync(_cts.Token));
            //_ = Task.Run(() => ConsumeOrdersAsync(_cts.Token));

            _produceTask = ProduceOrdersAsync(_cts.Token);
            _consumeTask = ConsumeOrdersAsync(_cts.Token);

            _ = Task.WhenAll(_produceTask, _consumeTask)
                .ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        _logger.LogError(t.Exception, "Background tasks crashed!");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void Stop()
        {
            _logger.LogInformation("Stopping background tasks...");
            _cts.Cancel();
        }

        private async Task ProduceOrdersAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var order = await _marketOrderSource.GetNextMarketOrderAsync();
                    await _channel.Writer.WriteAsync(order, cancellationToken);

                    _logger.LogInformation("New market order received: {InstrumentId} @ {Price} x {Quantity}",
                        order.InstrumentId, order.Price, order.Quantity);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ProduceOrdersAsync has been cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProduceOrdersAsync");
            }
        }

        private async Task ConsumeOrdersAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var order in _channel.Reader.ReadAllAsync(cancellationToken))
                {
                    var orders = _marketOrders.GetOrAdd(order.InstrumentId, _ => new List<MarketOrder>());
                    lock (orders)
                    {
                        orders.Add(order);
                    }

                    _logger.LogInformation("***Order stored for {InstrumentId}. Total Orders: {Count}",
                        order.InstrumentId, orders.Count);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ConsumeOrdersAsync has been cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConsumeOrdersAsync");
            }
        }

        public async Task<double> GetQuoteAsync(string instrumentId, int quantity)
        {
            await Task.Yield();

            if (!_marketOrders.TryGetValue(instrumentId, out var orders) || orders == null)
            {
                _logger.LogWarning("No orders found for instrument: {InstrumentId}", instrumentId);
                return 0;
            }

            List<MarketOrder> snapshot;
            lock (orders)
            {
                snapshot = orders.ToList();
            }

            var quote = _quoteStrategy.GetQuote(snapshot, quantity);

            if (quote < 0)
            {
                _logger.LogInformation("Insufficient orders to fulfill the quote for {InstrumentId} @ {Quantity}", instrumentId, quantity);
            }
            else
            {
                _logger.LogInformation("Quote calculated for {InstrumentId} @ Qty {Quantity} = {Quote}",
                    instrumentId, quantity, quote);
            }

            return quote;
        }

        public async Task<double> GetVolumeWeightedAveragePriceAsync(string instrumentId)
        {
            await Task.Yield();

            if (!_marketOrders.TryGetValue(instrumentId, out var orders) || orders == null)
            {
                _logger.LogWarning("No orders found for instrument: {InstrumentId}", instrumentId);
                return 0;
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

        public void Dispose()
        {
            Stop();
            _cts.Dispose();
        }
    }
}
