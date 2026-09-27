namespace Scanner.Core;
public record SessionAction(string Kind,int Crafts,Dictionary<string,decimal> Quantities,Dictionary<string,decimal> Prices,DateTimeOffset Time,decimal CraftingFee = 0);
public record CraftSession(string Id,string Name,string Server,string City,Recipe Recipe,Dictionary<string,decimal> UnitPrices,Dictionary<string,decimal> InitialInventory,decimal ExpectedSell,List<SessionAction> Actions,decimal CraftingFee = 0);
public record SessionTotals(Dictionary<string,decimal> Inventory,int Crafts,decimal Outputs,int AvailableCrafts,decimal Spent,decimal Gross,decimal Fee,decimal Net,decimal CashProfit,decimal? CashRoi,decimal LeftoverValue,decimal AdjustedCost,decimal AdjustedProfit,decimal? AdjustedRoi);
public static class SessionCalculator
{
 public static CraftSession Create(Recipe recipe,string server,string city,int initialCrafts,decimal expected,IReadOnlyDictionary<string,decimal> prices)
 {
  Calculator.Calculate(recipe,initialCrafts,expected,prices);
  if(recipe.Materials.Any(m=>!prices.ContainsKey(m.ItemId)))throw new ArgumentException("Save all material prices before starting a session.");
  return new(Guid.NewGuid().ToString("N"),recipe.Name+" · "+DateTime.Now.ToString("MMM d HH:mm"),server,city,recipe,recipe.Materials.ToDictionary(m=>m.ItemId,m=>prices[m.ItemId]),recipe.Materials.ToDictionary(m=>m.ItemId,m=>m.Quantity*initialCrafts),expected,[]);
 }
 public static SessionTotals Calculate(CraftSession s)
 {
  DataFiles.Validate(s.Recipe);
  if(s.ExpectedSell<0||s.ExpectedSell>1000000000000m)throw new ArgumentException("Invalid expected sell price.");
  if(s.CraftingFee<0||s.CraftingFee>1000000000000m)throw new ArgumentException("Crafting fee must be between 0 and 1,000,000,000,000 silver.");
  var inventory=new Dictionary<string,decimal>();decimal spent=s.CraftingFee;int crafts=0;
  foreach(var m in s.Recipe.Materials){if(!s.UnitPrices.TryGetValue(m.ItemId,out var price)||price<=0||price>1000000000000m||!s.InitialInventory.TryGetValue(m.ItemId,out var qty)||qty<0||qty>1000000000000m)throw new ArgumentException("Invalid session starting inventory or prices.");inventory[m.ItemId]=qty;spent+=qty*price;}
  foreach(var a in s.Actions)
  {
   if(a.CraftingFee<0||a.CraftingFee>1000000000000m || (a.Kind!="Round" && a.CraftingFee!=0))throw new ArgumentException("Invalid round crafting fee.");
   if(a.Quantities.Any(p=>!inventory.ContainsKey(p.Key)||p.Value<0||p.Value>1000000000000m||p.Value!=decimal.Truncate(p.Value)))throw new ArgumentException("Use nonnegative whole quantities for recipe materials only.");
   if(a.Kind=="Purchase")
   {
    if(!a.Quantities.Any(p=>p.Value>0))throw new ArgumentException("Enter a purchased quantity.");
    foreach(var p in a.Quantities){if(p.Value==0)continue;if(!a.Prices.TryGetValue(p.Key,out var price)||price<=0||price>1000000000000m)throw new ArgumentException("Enter a positive purchase unit price.");inventory[p.Key]+=p.Value;spent+=p.Value*price;}
   }
   else if(a.Kind=="Round")
   {
    if(a.Crafts<1||a.Crafts>1000000||crafts>1000000-a.Crafts)throw new ArgumentException("Crafts must be positive; maximum 1,000,000 per session.");
    foreach(var m in s.Recipe.Materials){var used=m.Quantity*a.Crafts;var returned=a.Quantities.GetValueOrDefault(m.ItemId);if(inventory[m.ItemId]<used)throw new ArgumentException("Not enough "+m.Name+". Reduce crafts or add purchased materials.");if(returned>inventory[m.ItemId])throw new ArgumentException("Entered "+m.Name+" exceeds the inventory available before this round. Record purchases separately.");inventory[m.ItemId]=returned;}
    crafts+=a.Crafts;spent+=s.Recipe.SilverCost*a.Crafts+a.CraftingFee;
   }
   else throw new ArgumentException("Unknown session action.");
  }
  var available=(int)Math.Min(1000000-crafts,s.Recipe.Materials.Min(m=>decimal.Floor(inventory[m.ItemId]/m.Quantity)));
  var outputs=(decimal)crafts*s.Recipe.OutputQuantity;var gross=outputs*s.ExpectedSell;var fee=gross*Calculator.ListingFee;var net=gross-fee;
  var leftovers=inventory.Sum(p=>p.Value*s.UnitPrices[p.Key]);var cost=spent-leftovers;
  return new(inventory,crafts,outputs,available,spent,gross,fee,net,net-spent,spent>0?(net-spent)/spent*100:null,leftovers,cost,net-cost,cost>0?(net-cost)/cost*100:null);
 }
}



