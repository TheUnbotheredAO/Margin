namespace Scanner.Core;
public record ReturnTotals(decimal? Value, decimal? AdjustedCost, decimal? Profit, decimal? Roi);
public static class ReturnCalculator
{
 public static ReturnTotals Calculate(Recipe recipe,int batch,decimal expected,IReadOnlyDictionary<string,decimal> prices,IReadOnlyDictionary<string,decimal> returned)
 {
  var baseline=Calculator.Calculate(recipe,batch,expected,prices);
  if(returned.Keys.Any(id=>!recipe.Materials.Any(m=>m.ItemId==id)))throw new ArgumentException("Return contains an unknown material.");
  decimal? value=0;
  foreach(var m in recipe.Materials)
  {
   var qty=returned.GetValueOrDefault(m.ItemId);
   if(qty<0 || qty>m.Quantity*batch || qty!=decimal.Truncate(qty))throw new ArgumentException("Returned quantities must be whole numbers from zero to the quantity used in this batch.");
   if(qty==0)continue;
   if(!prices.TryGetValue(m.ItemId,out var price)){value=null;continue;}
   value+=qty*price;
  }
  var cost=baseline.Cost-value;var profit=baseline.Net-cost;
  return new(value,cost,profit,cost>0?profit/cost*100:null);
 }
}
