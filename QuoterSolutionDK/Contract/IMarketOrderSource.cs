using QuoterSolutionDK.Entity;

namespace QuoterSolutionDK.Contract
{
    public interface IMarketOrderSource
    {
        Task<MarketOrder> GetNextMarketOrderAsync();
    }
}
