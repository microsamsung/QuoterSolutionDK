namespace QuoterSolutionDK.Contract
{

    public interface IQuoter
    {
        /// <summary>
        /// Returns best possible price for given instrument at given quantity.
        /// </summary>
        /// <param name="instrumentId">Instrumnet ID to quote, e.g. "DK50782120"</param>
        /// <param name="quantity">Quantity to quote for, e.g. 19</param>
        /// <returns>Best available total price to buy given quantity</returns>
        Task<double> GetQuoteAsync(string instrumentId, int quantity);


        /// <summary>
        /// Returns Volume-weighted average price for given instrument
        /// </summary>
        /// <param name="instrumentId">Instrument ID to return VWAP of, e.g. "DK50782120"</param>
        /// <returns>Current Volume-weighted average price</returns>
        Task<double> GetVolumeWeightedAveragePriceAsync(string instrumentId);
    }
}
