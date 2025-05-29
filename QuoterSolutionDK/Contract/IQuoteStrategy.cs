using QuoterSolutionDK.Entity;

namespace QuoterSolutionDK.Contract
{
    public interface IQuoteStrategy
    {
        double GetQuote(List<MarketOrder> orders, int quantity);
    }
}
