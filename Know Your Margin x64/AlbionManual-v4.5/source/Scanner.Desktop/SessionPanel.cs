using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Scanner.Core;
namespace Scanner.Desktop;
public sealed class SessionPanel:UserControl
{
 readonly string file; List<CraftSession> sessions; readonly Func<CraftSession> create;
 readonly ComboBox picker=new(){DisplayMemberPath="Name",Width=350};
 readonly TextBox crafts=new(){Width=110}, roundFee=new(){Width=150,Text="0"}, sale=new(){Width=150};
 readonly TextBlock info=new(){TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,16)}, summary=new(){TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,12)};
 readonly TextBlock salesProfit=new(), remainingValue=new(), combinedProfit=new(), roiSales=new(), roiCombined=new();
 readonly StackPanel materials=new(), purchases=new(); readonly ListBox history=new(){MaxHeight=200,MinHeight=80};
 readonly Dictionary<string,(TextBox Return,TextBox Buy,TextBox Price)> fields=new();
 readonly List<Button> actions=[]; bool rendering;
 CraftSession? Current=>picker.SelectedItem as CraftSession;
 public SessionPanel(string path,Func<CraftSession> create)
 {
  file=path;this.create=create;sessions=File.Exists(file)?DataFiles.Read<List<CraftSession>>(file):[];foreach(var s in sessions)SessionCalculator.Calculate(s);
  var body=new StackPanel();Content=body;
  body.Children.Add(new TextBlock{Text="Crafting session & returned materials",FontSize=23,FontWeight=FontWeights.SemiBold});
  body.Children.Add(new TextBlock{Text="Use New session setup above to choose starting stock. Each saved session keeps its own recipe and prices.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,12)});
  var top=new WrapPanel();top.Children.Add(picker);
  var add=new Button{Content="Start new session",Margin=new Thickness(10,0,0,0)};top.Children.Add(add);body.Children.Add(top);
  add.Click+=(_,_)=>Attempt(()=>{var s=create();SessionCalculator.Calculate(s);var next=sessions.Append(s).ToList();DataFiles.Save(file,next);sessions=next;Reload(s.Id);});
  picker.SelectionChanged+=(_,_)=>Render();
  var settings=new WrapPanel{Margin=new Thickness(0,12,0,0)};settings.Children.Add(Label("Expected sell / output item"));settings.Children.Add(sale);settings.Children.Add(Label("Saves when you finish editing"));body.Children.Add(settings);
  sale.LostKeyboardFocus+=(_,_)=>Attempt(SaveSale);
  body.Children.Add(info);
  var round=new WrapPanel();round.Children.Add(Label("Crafts this round"));round.Children.Add(crafts);round.Children.Add(Label("Crafting fee this round"));round.Children.Add(roundFee);body.Children.Add(round);
  body.Children.Add(new TextBlock{Text="Enter this round's additional silver fee. It is added once when you save the round. Enter your exact material balances after crafting below; they replace earlier quantities.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,14)});
  body.Children.Add(materials);
  body.Children.Add(Button("Save round",SaveRound));
  var buySection=new Expander{Header="Add purchased materials",Content=purchases,Margin=new Thickness(0,16,0,16)};body.Children.Add(buySection);
  body.Children.Add(new TextBlock{Text="Session results",FontSize=21,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,8,0,12)});
  var cards=new System.Windows.Controls.Primitives.UniformGrid{Columns=3};cards.Children.Add(Card("EXPECTED SALES PROFIT",salesProfit,"Net sales minus all session costs",roiSales));cards.Children.Add(Card("+ REMAINING MATERIAL VALUE",remainingValue,"Latest inventory at saved unit prices",new TextBlock{Text="Inventory value, not cash received."}));cards.Children.Add(Card("= COMBINED PROFIT",combinedProfit,"Sales profit plus remaining material value",roiCombined));body.Children.Add(cards);body.Children.Add(summary);
  var log=new Expander{Header="Saved rounds and purchases",IsExpanded=true,Margin=new Thickness(0,8,0,0)};var logBody=new StackPanel();logBody.Children.Add(history);logBody.Children.Add(Button("Undo last saved action",()=>{if(Current==null||Current.Actions.Count==0)return;if(MessageBox.Show(Window.GetWindow(this),"Undo the latest round (including its fee) or purchase?","Undo",MessageBoxButton.YesNo)==MessageBoxResult.Yes)Commit(Current with{Actions=Current.Actions.Take(Current.Actions.Count-1).ToList()});}));log.Content=logBody;body.Children.Add(log);
  body.Children.Add(new TextBlock{Text="Initial inputs and purchases are charged once. Recipe silver is charged per craft; round fees accumulate. Only the latest remaining inventory is credited. Premium sell-order estimates include 2.5% setup fee plus 4% sales tax. Relisting charges and game rounding are not modeled. Material valuation prices are frozen for each session. Save a round to keep its inputs; switching sessions discards unsaved round drafts.",FontSize=12,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,16,0,0)});
  Reload(sessions.LastOrDefault()?.Id);
 }
 static TextBlock Label(string text)=>new(){Text=text,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(10,0,10,0)};
 Button Button(string title,Action action){var b=new Button{Content=title,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,10,0,0)};b.Click+=(_,_)=>Attempt(action);actions.Add(b);return b;}
 static Border Card(string title,TextBlock value,string description,TextBlock roi){value.FontSize=27;value.FontWeight=FontWeights.SemiBold;value.Margin=new Thickness(0,10,0,10);roi.TextWrapping=TextWrapping.Wrap;var s=new StackPanel();s.Children.Add(new TextBlock{Text=title,FontSize=12,FontWeight=FontWeights.SemiBold});s.Children.Add(value);s.Children.Add(new TextBlock{Text=description,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10)});s.Children.Add(roi);return new(){Child=s,Background=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(27,47,54)),CornerRadius=new CornerRadius(8),Padding=new Thickness(16),Margin=new Thickness(0,0,10,0)};}
 void Attempt(Action a){try{a();}catch(Exception ex){MessageBox.Show(Window.GetWindow(this),ex.Message,"Session was not changed");}}
 static decimal Number(string text){if(string.IsNullOrWhiteSpace(text))return 0;if(!decimal.TryParse(text,NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingWhite|NumberStyles.AllowTrailingWhite,CultureInfo.CurrentCulture,out var n)||n<0||n>1000000000000m)throw new ArgumentException("Enter a nonnegative number without thousands separators.");return n;}
 void SaveSale(){if(rendering||Current==null)return;var price=Number(sale.Text);if(price==Current.ExpectedSell)return;var s=Current with{ExpectedSell=price};SessionCalculator.Calculate(s);var next=sessions.Select(x=>x.Id==s.Id?s:x).ToList();DataFiles.Save(file,next);var c=crafts.Text;var fee=roundFee.Text;var drafts=fields.ToDictionary(x=>x.Key,x=>(x.Value.Return.Text,x.Value.Buy.Text,x.Value.Price.Text));sessions=next;Reload(s.Id);crafts.Text=c;roundFee.Text=fee;foreach(var p in drafts){fields[p.Key].Return.Text=p.Value.Item1;fields[p.Key].Buy.Text=p.Value.Item2;fields[p.Key].Price.Text=p.Value.Item3;}}
 void SaveRound(){if(Current==null)return;if(!int.TryParse(crafts.Text,out var n))throw new ArgumentException("Enter a whole craft count.");var action=new SessionAction("Round",n,fields.ToDictionary(p=>p.Key,p=>Number(p.Value.Return.Text)),new(),DateTimeOffset.Now,Number(roundFee.Text));Commit(Current with{ExpectedSell=Number(sale.Text),Actions=Current.Actions.Append(action).ToList()});}
 void Commit(CraftSession s){SessionCalculator.Calculate(s);var next=sessions.Select(x=>x.Id==s.Id?s:x).ToList();DataFiles.Save(file,next);sessions=next;Reload(s.Id);}
 void Reload(string? id){rendering=true;picker.ItemsSource=sessions;picker.SelectedItem=sessions.FirstOrDefault(x=>x.Id==id);rendering=false;Render();}
 static string M(decimal? n)=>n?.ToString("N2")??"—";
 static Grid Row(params double[] widths){var g=new Grid{Margin=new Thickness(0,4,0,4)};foreach(var width in widths)g.ColumnDefinitions.Add(new(){Width=new GridLength(width,GridUnitType.Star)});return g;}
 static void Cells(Grid row,params UIElement[] cells){for(int i=0;i<cells.Length;i++){Grid.SetColumn(cells[i],i);row.Children.Add(cells[i]);}}
 static TextBlock Text(string text)=>new(){Text=text,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,8,6)};
 void Render(){rendering=true;try{materials.Children.Clear();purchases.Children.Clear();fields.Clear();foreach(var b in actions)b.IsEnabled=Current!=null;sale.IsEnabled=crafts.IsEnabled=roundFee.IsEnabled=Current!=null;var s=Current;if(s==null){info.Text="Open New session setup above, choose your starting batch, then select Start new session.";salesProfit.Text=remainingValue.Text=combinedProfit.Text="—";return;}
 var t=SessionCalculator.Calculate(s);sale.Text=s.ExpectedSell.ToString(CultureInfo.CurrentCulture);crafts.Text=Math.Max(1,t.AvailableCrafts).ToString();roundFee.Text="0";
 info.Text=$"{s.Recipe.Name} · {s.Server} / {s.City}\n{t.Crafts:N0} crafts completed · {t.Outputs:N0} items crafted · available materials support {t.AvailableCrafts:N0} more craft(s).";
 var head=Row(2,1,1,1.3,1.2,1.3);Cells(head,Text("Material"),Text("Available"),Text("Per craft"),Text("Return this round"),Text("Saved unit price"),Text("Remaining value"));materials.Children.Add(head);
 var buyHead=Row(2,1,1);Cells(buyHead,Text("Material"),Text("Buy extra qty"),Text("Buy unit cost"));purchases.Children.Add(buyHead);
 foreach(var m in s.Recipe.Materials){var ret=new TextBox{Text="0",Margin=new Thickness(0,0,10,0)};var buy=new TextBox{Text="0",Margin=new Thickness(0,0,10,0)};var price=new TextBox{Text=s.UnitPrices[m.ItemId].ToString(CultureInfo.CurrentCulture)};System.Windows.Automation.AutomationProperties.SetName(ret,m.Name+" return this round");System.Windows.Automation.AutomationProperties.SetName(buy,m.Name+" purchase quantity");System.Windows.Automation.AutomationProperties.SetName(price,m.Name+" purchase price");var row=Row(2,1,1,1.3,1.2,1.3);Cells(row,Text(m.Name),Text(t.Inventory[m.ItemId].ToString("G29")),Text(m.Quantity.ToString("G29")),ret,Text(M(s.UnitPrices[m.ItemId])),Text(M(t.Inventory[m.ItemId]*s.UnitPrices[m.ItemId])));materials.Children.Add(row);var purchase=Row(2,1,1);Cells(purchase,Text(m.Name),buy,price);purchases.Children.Add(purchase);fields[m.ItemId]=(ret,buy,price);}
 var purchaseButton=new Button{Content="Save material purchase",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,10,0,0)};purchaseButton.Click+=(_,_)=>Attempt(()=>{if(Current!=null)Commit(Current with{Actions=Current.Actions.Append(new SessionAction("Purchase",0,fields.ToDictionary(p=>p.Key,p=>Number(p.Value.Buy.Text)),fields.ToDictionary(p=>p.Key,p=>Number(p.Value.Price.Text)),DateTimeOffset.Now)).ToList()});});purchases.Children.Add(purchaseButton);
 salesProfit.Text=M(t.CashProfit);remainingValue.Text=M(t.LeftoverValue);combinedProfit.Text=M(t.AdjustedProfit);roiSales.Text="ROI excluding leftovers: "+(t.CashRoi.HasValue?M(t.CashRoi)+"%":"undefined");roiCombined.Text="ROI including leftovers: "+(t.AdjustedRoi.HasValue?M(t.AdjustedRoi)+"%":"undefined");
 var fees=s.CraftingFee+s.Actions.Sum(a=>a.CraftingFee);summary.Text=$"Materials + recipe silver: {M(t.Spent-fees)} · Crafting fees: {M(fees)} · Total cost: {M(t.Spent)}\nGross sales: {M(t.Gross)} − setup fee (2.5%): {M(t.Gross*Calculator.SetupFee)} − sales tax (4% Premium): {M(t.Gross*Calculator.PremiumSalesTax)} = net sales: {M(t.Net)}"+(s.CraftingFee>0?$"\nIncludes {M(s.CraftingFee)} in crafting fees saved before round-by-round fees were introduced.":"");
 history.ItemsSource=s.Actions.Select((a,i)=>$"{i+1}. {a.Time:g} · "+(a.Kind=="Round"?$"{a.Crafts} crafts · crafting fee {M(a.CraftingFee)} · returns: ":"Purchased: ")+string.Join("; ",s.Recipe.Materials.Select(m=>$"{m.Name}: {a.Quantities.GetValueOrDefault(m.ItemId):N0}"))).ToArray();
 }finally{rendering=false;}}
 public void SmokeTest(){var s=create();sessions=[s];DataFiles.Save(file,sessions);Reload(s.Id);var ids=s.Recipe.Materials.Select(m=>m.ItemId).ToArray();crafts.Text="10";roundFee.Text="1000";fields[ids[0]].Return.Text="60";fields[ids[1]].Return.Text="30";fields[ids[2]].Return.Text="14";SaveRound();if(crafts.Text!="2"||roundFee.Text!="0")throw new Exception("Next round defaults failed");crafts.Text="2";roundFee.Text="200";fields[ids[0]].Return.Text="23";fields[ids[1]].Return.Text="12";fields[ids[2]].Return.Text="5";SaveRound();var t=SessionCalculator.Calculate(Current!);if(t.Inventory[ids[0]]!=23||Current!.Actions.Sum(a=>a.CraftingFee)!=1200||salesProfit.Text!=M(t.CashProfit)||combinedProfit.Text!=M(t.AdjustedProfit))throw new Exception("Round results failed");fields[ids[0]].Return.Text="7";sale.Text="11000";SaveSale();if(fields[ids[0]].Return.Text!="7"||DataFiles.Read<List<CraftSession>>(file).Single().ExpectedSell!=11000)throw new Exception("Sell price save failed");}
}

