
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Scanner.Core;

namespace Scanner.Desktop;
public partial class MainWindow : Window
{
    readonly string config = Path.Combine(AppContext.BaseDirectory, "config");
    ManualPriceStore manualStore;
    List<Recipe> recipes;
    Settings settings;
    ManualPriceStore saleStore;

    Dictionary<string, decimal> overrides = new();


    bool ready;
    Recipe? Current => RecipePicker.SelectedItem as Recipe;
    public MainWindow()
    {
        InitializeComponent();
        manualStore = new ManualPriceStore(Path.Combine(config, "manual-prices.json"));
        saleStore = new ManualPriceStore(Path.Combine(config, "sell-prices.json"));
        recipes = DataFiles.Read<List<Recipe>>(Path.Combine(config, "recipes.json"));
        if (recipes.Count == 0) throw new InvalidDataException("No recipes found in config/recipes.json.");
        foreach (var recipe in recipes) DataFiles.Validate(recipe);
        if (recipes.Select(r => r.Key).Distinct().Count() != recipes.Count) throw new InvalidDataException("Duplicate recipe keys in recipes.json.");
        settings = DataFiles.Read<Settings>(Path.Combine(config, "settings.json"));
        if (!MarketClient.Servers.ContainsKey(settings.Server) || !MarketClient.Cities.Contains(settings.City) || settings.StaleMinutes is < 1 or > 10080)
            throw new InvalidDataException("Invalid settings.json: check server, city and StaleMinutes (1–10080).");
        Server.ItemsSource = MarketClient.Servers.Keys; Server.SelectedItem = settings.Server;
        City.ItemsSource = MarketClient.Cities; City.SelectedItem = settings.City;
        Category.ItemsSource = new[] { "All categories" }.Concat(recipes.Select(r => r.Category).Distinct().Order()).ToArray(); Category.SelectedIndex = 0;
        Tier.ItemsSource = new[] { "All tiers", "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8" }; Tier.SelectedIndex = 0;
        Enchant.ItemsSource=new[]{"All enchantments",".0 — Normal",".1",".2",".3",".4"}; Enchant.SelectedIndex=0;
        ready = true;
        FilterRecipes("T3_2H_CROSSBOW");
        InitializeSessions();
        NewSessionSetup.IsExpanded=!File.Exists(Path.Combine(config,"crafting-sessions.json")) || DataFiles.Read<List<CraftSession>>(Path.Combine(config,"crafting-sessions.json")).Count==0;

    }
    void FilterRecipes(string? preferred = null)
    {
        if (!ready) return;
        var category = Category.SelectedItem as string;
        var tier = Tier.SelectedIndex;
        var search = Search.Text.Trim();
        var selected = preferred ?? Current?.Key;
        var matches = recipes.Where(r => (category == "All categories" || r.Category == category) && (tier == 0 || r.Tier == tier) && (Enchant.SelectedIndex==0 || MaterialCatalog.EnchantmentOf(r.ItemId)==Enchant.SelectedIndex-1) && (r.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || r.ItemId.Contains(search, StringComparison.OrdinalIgnoreCase))).OrderBy(r => r.Tier).ThenBy(r=>MaterialCatalog.EnchantmentOf(r.ItemId)).ThenBy(r => r.Name).ToList();
        RecipePicker.ItemsSource = matches;
        RecipePicker.SelectedItem = matches.FirstOrDefault(r => r.Key == selected || r.ItemId == selected) ?? matches.FirstOrDefault();
        EditButton.IsEnabled = Current != null; ManualButton.IsEnabled = Current != null;
        Render();
    }
    void FilterChanged(object sender, SelectionChangedEventArgs e) => FilterRecipes();
    void SearchChanged(object sender, TextChangedEventArgs e) => FilterRecipes();
    void RecipeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready) return;
        LoadManualPrices(); LoadSellPrice();
        Status.Text = Current == null ? "No matching recipes. Change your filters or add a recipe." : "Saved prices loaded. Edit prices only when you want to update them.";
        Render();
    }
    void ContextChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready) return;
        overrides.Clear();
        settings = settings with { Server = (string)Server.SelectedItem, City = (string)City.SelectedItem };
        LoadManualPrices(); LoadSellPrice();
        try { DataFiles.Save(Path.Combine(config, "settings.json"), settings); Status.Text = "Saved prices loaded for the selected market."; }
        catch (Exception ex) { Status.Text = "Selection applied but could not save settings: " + ex.Message; }
        Render();
    }
    void InputsChanged(object sender, TextChangedEventArgs e) { if (ready) Render(); }
    bool Inputs(out int batch, out decimal expected)
    {
        var b = int.TryParse(Batch.Text, NumberStyles.None, CultureInfo.CurrentCulture, out batch) && batch is >= 1 and <= 1000000;
        var p = decimal.TryParse(Expected.Text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.CurrentCulture, out expected) && expected is >= 0 and <= 1000000000000m;
        Validation.Text = !b ? "Enter a whole batch quantity from 1 to 1,000,000." : !p ? "Enter a nonnegative sale value using your local decimal separator, without thousands separators." : expected == 0 ? "Enter your expected sell value per output item to estimate revenue." : "";
        return b && p;
    }
    static string Money(decimal? n) => n?.ToString("N2") ?? "—";
    void Render()
    {
        if (!ready) return;
        CityBonusHeading.Text = settings.City.ToUpperInvariant()+" · LOCAL PRODUCTION BONUSES";
        CityBonusText.Text = CityBonuses.For(settings.City);
        var valid = Inputs(out var batch, out var expected);
        var r = Current;
        RecipeInfo.Text = r == null ? "No matching recipes." : $"{r.Name}  •  T{r.Tier}  •  {r.OutputQuantity:N0} output item(s) per craft  •  {r.Source}  •  Direct silver/craft: {r.SilverCost:N2}";
        OutputCount.Text = r != null && valid ? $"{batch:N0} crafts × {r.OutputQuantity:N0} output = {(long)batch * r.OutputQuantity:N0} items sold" : "";
        MaterialsGrid.ItemsSource = r?.Materials.Select(m => new {
            Material = m.Name, PerCraft = m.Quantity.ToString("G29"),
            Quantity = valid ? (m.Quantity * batch).ToString("G29") : "—", City = settings.City,
            Used = overrides.TryGetValue(m.ItemId, out var price) ? Money(price) : "Not entered",
            Cost = valid && overrides.TryGetValue(m.ItemId, out var unit) ? Money(m.Quantity * batch * unit) : "—"
        }).ToList();
        var missing = r?.Materials.Count(m => !overrides.ContainsKey(m.ItemId)) ?? 0;
        MaterialTotal.Text = r != null && valid && missing == 0 ? Money(r.Materials.Sum(m => m.Quantity * batch * overrides[m.ItemId])) : "—";
        MaterialTotal.ToolTip = missing > 0 ? "Save every material price to calculate the total." : "Materials only for the selected starting batch; excludes crafting and market fees.";
        Freshness.Text = $"{r?.Materials.Count - missing ?? 0} saved · {missing} not entered";
        QuoteWarning.Text = "Your prices stay saved until you personally update or clear them. This app makes no market requests.";
        OutputMarket.Text = "Prices are stored separately for each server, city and item. The same material price is reused across recipes.";
        if (missing > 0) Validation.Text += " Enter the missing material prices to calculate cost and profit.";        var t = r != null && valid ? Calculator.Calculate(r, batch, expected, overrides) : null;
        Cost.Text = Money(t?.Cost); Gross.Text = Money(t?.Gross); Fee.Text = Money(t?.Fee); Net.Text = Money(t?.Net); Profit.Text = Money(t?.Profit); Roi.Text = t?.Roi is decimal roi ? roi.ToString("N2") + "%" : "—";

        Profit.Foreground = new SolidColorBrush(t?.Profit >= 0 ? Color.FromRgb(101, 214, 190) : Color.FromRgb(255, 153, 136));

    }
void LoadManualPrices() => overrides = Current == null ? new() : manualStore.Get(settings.Server, settings.City, Current.Materials.Select(m => m.ItemId));
    void ManualPrices(object sender, RoutedEventArgs e)
    {
        if (Current == null) return;
        var dialog = new PriceEditor(Current.Materials, overrides) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                manualStore.Save(settings.Server, settings.City, Current.Materials.Select(m => m.ItemId), dialog.Result);
                LoadManualPrices(); Render();
                Status.Text = "Manual prices saved for " + settings.Server + " / " + settings.City + ". These prices remain saved until you update or clear them.";
            }
            catch (Exception ex) { MessageBox.Show(this, "Manual prices were not saved: " + ex.Message); }
        }
    }

    void EditRecipe(object sender, RoutedEventArgs e) { if (Current != null) OpenEditor(Current); }
    void NewRecipe(object sender, RoutedEventArgs e) => OpenEditor(null);
    void OpenEditor(Recipe? original)
    {
        var editor = new RecipeEditor(original, recipes) { Owner = this };
        if (editor.ShowDialog() != true || editor.Result is not Recipe result) return;
        if (recipes.Any(r => r.Key == result.Key && r != original)) { MessageBox.Show(this, "This item already has a recipe. Edit that recipe instead."); return; }
        var next = recipes.Where(r => r != original).Append(result).ToList();
        try { DataFiles.Save(Path.Combine(config, "recipes.json"), next); }
        catch (Exception ex) { MessageBox.Show(this, "Recipe was not saved: " + ex.Message); return; }
        recipes = next;
        Category.ItemsSource = new[] { "All categories" }.Concat(recipes.Select(r => r.Category).Distinct().Order()).ToArray(); Category.SelectedIndex = 0;
        Tier.SelectedIndex = 0; Enchant.SelectedIndex=0; Search.Text = ""; FilterRecipes(result.Key);
        Status.Text = "Recipe saved. Saved material prices are ready to use.";
    }
    void LoadSellPrice()
    {
        var saved = Current == null ? new Dictionary<string,decimal>() : saleStore.Get(settings.Server, settings.City, [Current.ItemId]);
        Expected.Text = Current != null && saved.TryGetValue(Current.ItemId, out var price) ? price.ToString(CultureInfo.CurrentCulture) : "0";
    }
    void EditSellPrice(object sender, RoutedEventArgs e)
    {
        if (Current == null) return;
        var id = Current.ItemId;
        var dialog = new PriceEditor([new Material(id, Current.Name + " — expected sell price", 1)], saleStore.Get(settings.Server, settings.City, [id])) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try { saleStore.Save(settings.Server, settings.City, [id], dialog.Result); LoadSellPrice(); Render(); Status.Text = "Expected sell price saved until you update or clear it."; }
        catch (Exception ex) { MessageBox.Show(this, "Sell price was not saved: " + ex.Message); }
    }
    void InitializeSessions()
    {
        SessionHost.Content=new SessionPanel(Path.Combine(config,"crafting-sessions.json"),()=>{
            if(Current==null || !Inputs(out var batch,out var expected))throw new ArgumentException("Select a recipe and enter a valid starting batch above first.");
            var session=SessionCalculator.Create(Current,settings.Server,settings.City,batch,expected,overrides); NewSessionSetup.IsExpanded=false; return session;
        });
    }
    void OpenMaterials(object sender,RoutedEventArgs e){var window=new MaterialsWindow(recipes,manualStore,settings.Server,settings.City,()=>{LoadManualPrices();Render();Status.Text="Material prices updated from Materials.";}){Owner=this};window.ShowDialog();}
    void OpenSessions(object sender,RoutedEventArgs e)=>SessionHost.BringIntoView();    public void SmokeTest()
    {
        if (Current == null || MaterialsGrid.Items.Count != 2) throw new Exception("Initial recipe failed.");
        Batch.Text = "10"; Expected.Text = "1000";
        overrides = new() { ["T3_PLANKS"] = 10, ["T3_METALBAR"] = 20 }; Render();
        if (Cost.Text != Money(4400) || Fee.Text != Money(650) || Profit.Text != Money(4950)) throw new Exception("Manual calculation failed.");
        if(SessionHost.Content is not SessionPanel)throw new Exception("Integrated session missing.");
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "smoke-test.txt"), "PASS: manual UI calculation.");
    }
}













