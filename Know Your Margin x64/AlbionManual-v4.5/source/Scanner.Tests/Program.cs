using Scanner.Core;
var count=0;
void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS: "+name);count++;}
var recipes=DataFiles.Read<List<Recipe>>(args[0]);foreach(var r in recipes)DataFiles.Validate(r);
Check(recipes.Count==12917,"complete catalog validates");
var recipe=recipes.Single(r=>r.ItemId=="T3_2H_CROSSBOW");
var prices=new Dictionary<string,decimal>{["T3_PLANKS"]=10,["T3_METALBAR"]=20};
var t=Calculator.Calculate(recipe,10,1000,prices);
Check(t.Cost==4400 && t.Gross==10000 && t.Fee==650 && t.Net==9350 && t.Profit==4950 && t.Roi==112.5m,"manual batch cost, 6.5% fee, profit and ROI");
Check(Calculator.Calculate(recipe,1,1000,new Dictionary<string,decimal>()).Cost==null,"missing prices never treated as free");
Check(Calculator.Calculate(recipe with{OutputQuantity=10,SilverCost=100},2,100,prices).Cost==1080,"recipe silver included");
Check(Calculator.Calculate(recipe with{OutputQuantity=10},2,100,prices).Gross==2000,"multi-output batch revenue");
var folder=Path.Combine(Path.GetTempPath(),"albion-offline-"+Guid.NewGuid());Directory.CreateDirectory(folder);
var materialPath=Path.Combine(folder,"manual-prices.json");var salePath=Path.Combine(folder,"sell-prices.json");
try{
 var store=new ManualPriceStore(materialPath);store.Save("Americas","Martlock",prices.Keys,prices);
 var sell=new ManualPriceStore(salePath);sell.Save("Americas","Martlock",[recipe.ItemId],new Dictionary<string,decimal>{[recipe.ItemId]=1000});
 Check(new ManualPriceStore(materialPath).Get("Americas","Martlock",prices.Keys)["T3_PLANKS"]==10,"material prices survive restart");
 Check(new ManualPriceStore(salePath).Get("Americas","Martlock",[recipe.ItemId])[recipe.ItemId]==1000,"sell price survives restart");
 Check(store.Get("Asia","Martlock",prices.Keys).Count==0 && store.Get("Americas","Bridgewatch",prices.Keys).Count==0,"market isolation");
 for(var i=0;i<100;i++)Calculator.Calculate(recipe,10,1000,store.Get("Americas","Martlock",prices.Keys));
 Check(new ManualPriceStore(materialPath).Get("Americas","Martlock",prices.Keys)["T3_PLANKS"]==10,"calculations do not modify saved prices");
 store.Save("Americas","Martlock",["T3_PLANKS"],new Dictionary<string,decimal>{["T3_PLANKS"]=288});
 Check(new ManualPriceStore(materialPath).Get("Americas","Martlock",prices.Keys)["T3_METALBAR"]==20,"updating one item preserves others");
 Check(new ManualPriceStore(materialPath).Get("Americas","Martlock",prices.Keys)["T3_PLANKS"]==288,"personal update persists");
 store.Save("Americas","Martlock",["T3_PLANKS"],new Dictionary<string,decimal>());
 Check(new ManualPriceStore(materialPath).Get("Americas","Martlock",["T3_PLANKS"]).Count==0,"explicit clear persists");
}finally{foreach(var f in Directory.GetFiles(folder))File.Delete(f);Directory.Delete(folder);}
Check(!typeof(Calculator).Assembly.GetReferencedAssemblies().Any(a=>a.Name=="System.Net.Http"),"core has no HTTP dependency");
var potion=new Recipe("T5_POTION","Resistance potion","Potion",5,1,[new("T5_TEASEL","Dragon Teasel",24),new("T3_BURDOCK","Burdock",12),new("T4_MILK","Milk",6)],"test");
var pp=new Dictionary<string,decimal>{["T5_TEASEL"]=100,["T3_BURDOCK"]=200,["T4_MILK"]=300};
var ret=new Dictionary<string,decimal>{["T5_TEASEL"]=11,["T3_BURDOCK"]=5,["T4_MILK"]=3};
var adjusted=ReturnCalculator.Calculate(potion,1,10000,pp,ret);
Check(adjusted.Value==3000 && adjusted.AdjustedCost==3600 && adjusted.Profit==5750 && adjusted.Roi==5750m/3600*100,"24/12/6 used and 11/5/3 returned example");
var noReturns=ReturnCalculator.Calculate(potion,1,10000,pp,new Dictionary<string,decimal>());
Check(noReturns.Value==0 && noReturns.Profit==Calculator.Calculate(potion,1,10000,pp).Profit,"zero returns matches baseline");
Check(ReturnCalculator.Calculate(potion,2,10000,pp,ret).Value==3000,"returned quantities are absolute batch totals");
Check(ReturnCalculator.Calculate(potion with{SilverCost=100},1,10000,pp,ret).AdjustedCost==3700,"direct silver cost is not refunded");
Check(ReturnCalculator.Calculate(potion,1,10000,new Dictionary<string,decimal>(),ret).Profit==null,"missing saved prices block adjusted profit");
var all=potion.Materials.ToDictionary(m=>m.ItemId,m=>m.Quantity);
Check(ReturnCalculator.Calculate(potion,1,10000,pp,all).Roi==null,"full return leaves zero cost and undefined ROI");
void Reject(decimal qty,string label){try{ReturnCalculator.Calculate(potion,1,10000,pp,new Dictionary<string,decimal>{["T5_TEASEL"]=qty});throw new Exception("Accepted invalid return");}catch(ArgumentException){Check(true,label);}}
Reject(-1,"reject negative returned quantity");Reject(25,"reject returns exceeding batch usage");Reject(1.5m,"reject fractional resource count");
var snow=SessionCalculator.Create(potion,"Americas","Martlock",10,10000,pp);
SessionAction Round(int crafts,decimal a,decimal b,decimal c)=>new("Round",crafts,new(){["T5_TEASEL"]=a,["T3_BURDOCK"]=b,["T4_MILK"]=c},new(),DateTimeOffset.Now);
snow=snow with{Actions=[Round(10,59,30,15)]};var first=SessionCalculator.Calculate(snow);
Check(first.AvailableCrafts==2 && first.Inventory["T5_TEASEL"]==59,"first round uses exact entered balance");
snow=snow with{Actions=snow.Actions.Append(Round(2,23,12,6)).ToList()};var second=SessionCalculator.Calculate(snow);
Check(second.Crafts==12 && second.Inventory["T5_TEASEL"]==23 && second.Inventory["T3_BURDOCK"]==12 && second.Inventory["T4_MILK"]==6,"59/30/15 then 23/12/6 replaces balances without carryover");
Check(second.AvailableCrafts==0,"23 Teasel does not allow a 24-Teasel craft");
Check(second.Spent==66000 && second.LeftoverValue==6500 && second.AdjustedCost==59500,"only latest inventory valued, purchased inputs charged once");
Check(second.Gross==120000 && second.Fee==7800 && second.CashProfit==46200 && second.AdjustedProfit==52700,"profit cards use latest balance and unchanged fee");
try{SessionCalculator.Calculate(snow with{Actions=snow.Actions.Append(Round(1,0,0,0)).ToList()});throw new Exception("Overspending inventory allowed");}catch(ArgumentException){Check(true,"blocks crafting beyond exact latest inventory");}
var top=new SessionAction("Purchase",0,new(){["T5_TEASEL"]=24,["T3_BURDOCK"]=12,["T4_MILK"]=6},new(){["T5_TEASEL"]=110,["T3_BURDOCK"]=220,["T4_MILK"]=330},DateTimeOffset.Now);
var bought=snow with{Actions=snow.Actions.Append(top).ToList()};var bt=SessionCalculator.Calculate(bought);
Check(bt.Spent==73260 && bt.AvailableCrafts==1 && bt.Inventory["T5_TEASEL"]==47,"explicit purchases still add stock and cost");
Check(bt.LeftoverValue==13100,"top-up inventory uses snapshot valuation");
var last=SessionCalculator.Calculate(bought with{Actions=bought.Actions.Append(Round(1,11,6,1)).ToList()});
Check(last.Inventory["T5_TEASEL"]==11 && last.Inventory["T4_MILK"]==1,"third round replaces purchased and previous balances");
var saved=System.Text.Json.JsonSerializer.Serialize(snow,DataFiles.Json);var restored=System.Text.Json.JsonSerializer.Deserialize<CraftSession>(saved,DataFiles.Json)!;
Check(SessionCalculator.Calculate(restored).AdjustedProfit==second.AdjustedProfit,"saved sessions retain replacement semantics");
Check(SessionCalculator.Calculate(snow with{Actions=snow.Actions.Take(1).ToList()}).Inventory["T5_TEASEL"]==59,"undo restores prior entered balance");
Check(SessionCalculator.Calculate(snow with{Recipe=potion with{SilverCost=100}}).Spent==67200,"direct silver remains per craft");
var partial=SessionCalculator.Create(potion,"Americas","Martlock",10,10000,pp) with{Actions=[Round(1,220,110,55)]};
Check(SessionCalculator.Calculate(partial).Inventory["T5_TEASEL"]==220,"balance may exceed one-round consumption when stock remains");
var empty=SessionCalculator.Calculate(snow with{Actions=[Round(10,0,0,0)]});Check(empty.LeftoverValue==0 && empty.AvailableCrafts==0,"zero entries replace inventory with zero");
var sessionsFile=Path.Combine(Path.GetDirectoryName(args[0])!,"crafting-sessions.json");
if(File.Exists(sessionsFile)){foreach(var existing in DataFiles.Read<List<CraftSession>>(sessionsFile))SessionCalculator.Calculate(existing);Check(true,"existing saved sessions recalculate successfully");}
var charged=snow with{CraftingFee=1000};var ct=SessionCalculator.Calculate(charged);
Check(ct.Spent==second.Spent+1000 && ct.AdjustedCost==second.AdjustedCost+1000,"session fee counted once across all rounds");
Check(ct.CashProfit==second.CashProfit-1000 && ct.AdjustedProfit==second.AdjustedProfit-1000,"fee reduces both profit figures");
Check(ct.CashRoi==ct.CashProfit/ct.Spent*100 && ct.AdjustedRoi==ct.AdjustedProfit/ct.AdjustedCost*100,"both ROI denominators include fee");
Check(ct.Gross==second.Gross && ct.Fee==second.Fee && ct.LeftoverValue==second.LeftoverValue,"crafting fee leaves listing fee and inventory valuation unchanged");
var feeJson=System.Text.Json.JsonSerializer.Serialize(charged,DataFiles.Json);
Check(System.Text.Json.JsonSerializer.Deserialize<CraftSession>(feeJson,DataFiles.Json)!.CraftingFee==1000,"session fee survives serialization");
var legacy=System.Text.Json.Nodes.JsonNode.Parse(feeJson)!.AsObject();legacy.Remove("craftingFee");legacy.Remove("CraftingFee");
Check(System.Text.Json.JsonSerializer.Deserialize<CraftSession>(legacy.ToJsonString(),DataFiles.Json)!.CraftingFee==0,"legacy sessions default fee to zero");
Check(SessionCalculator.Calculate(charged with{CraftingFee=2000}).Spent==second.Spent+2000,"editing fee replaces previous total");
Check(SessionCalculator.Calculate(charged with{CraftingFee=0}).Spent==second.Spent,"clearing fee to zero restores original cost");
foreach(var bad in new[]{-1m,1000000000001m}){try{SessionCalculator.Calculate(snow with{CraftingFee=bad});throw new Exception("Invalid fee accepted");}catch(ArgumentException){Check(true,"invalid crafting fee rejected");}}
var withRounds=snow with{Actions=snow.Actions.Select((a,i)=>a with{CraftingFee=i==0?1000:200}).ToList()};var rt=SessionCalculator.Calculate(withRounds);
Check(rt.Spent==second.Spent+1200 && rt.CashProfit==second.CashProfit-1200 && rt.AdjustedProfit==second.AdjustedProfit-1200,"round fees accumulate once and reduce both profits");
Check(SessionCalculator.Calculate(withRounds with{Actions=withRounds.Actions.Take(1).ToList()}).Spent==first.Spent+1000,"undo removes latest round fee");
Check(SessionCalculator.Calculate(withRounds with{CraftingFee=500}).Spent==second.Spent+1700,"legacy session fee retained alongside new round fees");
Check(SessionCalculator.Calculate(System.Text.Json.JsonSerializer.Deserialize<CraftSession>(System.Text.Json.JsonSerializer.Serialize(withRounds,DataFiles.Json),DataFiles.Json)!).Spent==rt.Spent,"round fees survive save and reload");
Check(100000m*Calculator.SetupFee==2500 && 100000m*Calculator.PremiumSalesTax==4000 && 100000m*Calculator.ListingFee==6500,"setup fee and Premium tax both apply to gross sale");
try{SessionCalculator.Calculate(snow with{Actions=[Round(10,60,30,15) with{CraftingFee=-1}]});throw new Exception("Negative fee accepted");}catch(ArgumentException){Check(true,"negative round fee rejected");}
var materialCatalog=MaterialCatalog.FromRecipes(recipes);
Check(!materialCatalog.Any(m=>m.ItemId.Contains("ARTEFACT")||m.ItemId.Contains("ARTIFACT")),"material catalog excludes crafting artifacts");
Check(MaterialCatalog.Filter(materialCatalog,5,3,"").Any(m=>m.ItemId=="T5_PLANKS_LEVEL3@3") && MaterialCatalog.Filter(materialCatalog,5,3,"").All(m=>m.Tier==5&&m.Enchantment==3),"T5.3 material filter includes correct planks only");
Check(MaterialCatalog.Filter(materialCatalog,4,0,"").All(m=>m.Tier==4&&m.Enchantment==0),"normal material filter excludes enchanted resources");
Check(MaterialCatalog.Filter(materialCatalog,0,-1,"").Count()==materialCatalog.Count,"all filters retain all non-artifact ingredients");
Check(MaterialCatalog.GroupOf("T5_WOOD_LEVEL3@3")=="Raw Resources" && MaterialCatalog.GroupOf("T5_PLANKS_LEVEL3@3")=="Refined Resources","enchanted logs and planks have distinct material groups");
Check(MaterialCatalog.GroupOf("T4_MILK")=="Other Ingredients" && MaterialCatalog.GroupOf("T5_TEASEL")=="Other Ingredients","milk and herbs remain other ingredients");
var dateFolder=Path.Combine(Path.GetTempPath(),"albion-dates-"+Guid.NewGuid());Directory.CreateDirectory(dateFolder);
try{var datePath=Path.Combine(dateFolder,"prices.json");DataFiles.Save(datePath,new Dictionary<string,decimal>{["Americas|Martlock|T5_PLANKS"]=100});var ds=new ManualPriceStore(datePath);Check(ds.UpdatedAt("Americas","Martlock","T5_PLANKS")==null,"older prices have unknown dates");ds.Save("Americas","Martlock",["T5_PLANKS"],new Dictionary<string,decimal>{["T5_PLANKS"]=200});var stamp=ds.UpdatedAt("Americas","Martlock","T5_PLANKS");Check(stamp!=null && new ManualPriceStore(datePath).UpdatedAt("Americas","Martlock","T5_PLANKS")==stamp,"changed price date persists");Check(ds.UpdatedAt("Asia","Martlock","T5_PLANKS")==null,"price dates respect server");ds.Save("Americas","Martlock",["T5_PLANKS"],new Dictionary<string,decimal>());Check(ds.UpdatedAt("Americas","Martlock","T5_PLANKS")==null,"cleared price clears its date");}finally{foreach(var f in Directory.GetFiles(dateFolder))File.Delete(f);Directory.Delete(dateFolder);}
Console.WriteLine($"{count} checks passed.");




