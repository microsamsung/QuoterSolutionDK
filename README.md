# QuoterSolutionDK_WithConsole
Market Quoter Component

Your task is to implement market quoter component defined in *IQuoter* interface.

You have two methods to implement in *YourQuoter.cs*:
1. GetQuote
  - Takes instrument and quantity to quote, returns the best possible price with current quotes
2. GetVolumeWeightedAveragePrice
  - Takes instrument id and calculates volume-weighted average price for the instrument
  - More about: https://en.wikipedia.org/wiki/Volume-weighted_average_price

*IMarketOrderSource.cs*
You should depend on IMarketOrderSource interface as stand-in for market data feed. Keep in mind that
IMarketOrderSource.GetNextMarketOrder() blocks your call until next order is available, source is potentially endless.
You can use provided implementation of IQuoteSource as example or you can write your own.
You should not change IMarketOrderSource.cs in a significant way

*MarketOrder.cs*
Each individual market order is represented in MarketOrder class and has InstrumentId, Quantity at available at Price.
There can be many market orders for the same instrument with different quantities and different prices.

*Implementation Notes*
- Given implementation HardcodedQuoteSource of IMarketOrderSource simulated situation where there is limited number of 
- orders available
- You are welcome to make your own implementation of IMarketOrderSource interface
- Consider implementation of IMarketOrderSource where it would be giving orders as they are created throughout the day in
- an open market
- For more advanced cases consider decoupling flow of getting orders from flow of getting quotes
- You are welcome to add (or not) any test that you see fit
