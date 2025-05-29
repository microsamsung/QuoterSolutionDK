using QuoterSolutionDK.Contract;
using QuoterSolutionDK.Entity;

namespace QuoterSolutionDK
{
    public class BestPriceStrategy : IQuoteStrategy
    {
        public double GetQuote(List<MarketOrder> orders, int quantity)
        {
            var sortedOrders = orders.OrderBy(o => o.Price).ToList();
            //int remaining = quantity;
            double total = 0;

            foreach (var order in sortedOrders)
            {
                if (quantity <= 0) break;
                int take = System.Math.Min(order.Quantity, quantity);
                total += take * order.Price;
                quantity -= take;
            }

            return quantity > 0 ? -1 : total;
        }
    }
}
