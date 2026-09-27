using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Scanner.Core;

public record Material(string ItemId, string Name, decimal Quantity);
public record Recipe(string ItemId, string Name, string Category, int Tier, int OutputQuantity, List<Material> Materials, string Source, int Variant = 1, decimal SilverCost = 0) { public string Key => ItemId + "#" + Variant; }
public record Settings(string Server = "Americas", string City = "Bridgewatch", int StaleMinutes = 120);
public static class DataFiles
{
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    public static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) ?? throw new InvalidDataException($"Empty configuration: {path}");
    public static void Save<T>(string path, T value)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, Json));
        File.Move(temp, path, true);
    }
    public static bool ValidId(string? id) => id != null && Regex.IsMatch(id, @"^[A-Z][A-Z0-9_]{1,99}(?:@[1-4])?$");
    public static void Validate(Recipe r)
    {
        if (r.Variant < 1 || r.SilverCost < 0 || r.SilverCost > 1000000000000m || !ValidId(r.ItemId) || string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Category) || r.Tier < 1 || r.Tier > 8 || r.OutputQuantity < 1 || r.OutputQuantity > 100000)
            throw new InvalidDataException("Recipe needs a valid item ID, name, category, tier 1–8 and output quantity 1–100,000.");
        if (r.Materials == null || r.Materials.Count < 1 || r.Materials.Count > 10) throw new InvalidDataException("A recipe must contain 1–10 materials.");
        if (r.Materials.Any(m => m == null || !ValidId(m.ItemId) || string.IsNullOrWhiteSpace(m.Name) || m.Quantity <= 0 || m.Quantity > 1000000))
            throw new InvalidDataException("Each material needs a valid item ID, name and positive quantity (maximum 1,000,000).");
        if (r.Materials.Select(m => m.ItemId).Distinct().Count() != r.Materials.Count) throw new InvalidDataException("Combine duplicate materials into one row.");
    }
}
public record Totals(decimal? Cost, decimal Gross, decimal Fee, decimal Net, decimal? Profit, decimal? Roi);
public static class Calculator
{
    public const decimal SetupFee = 0.025m; public const decimal PremiumSalesTax = 0.04m; public const decimal ListingFee = SetupFee + PremiumSalesTax;
    public static Totals Calculate(Recipe recipe, int batch, decimal expectedPerItem, IReadOnlyDictionary<string, decimal> overrides)
    {
        DataFiles.Validate(recipe);
        if (batch < 1 || batch > 1000000 || expectedPerItem < 0 || expectedPerItem > 1000000000000m) throw new ArgumentOutOfRangeException(nameof(batch), "Batch must be 1–1,000,000 and sale value 0–1,000,000,000,000.");
        decimal? cost = recipe.SilverCost * batch;
        foreach (var m in recipe.Materials)
        {
            if (overrides != null && overrides.TryGetValue(m.ItemId, out var manual))
            {
                if (manual <= 0 || manual > 1000000000000m) throw new ArgumentOutOfRangeException(nameof(overrides));
                cost += m.Quantity * batch * manual;
                continue;
            }
            cost = null; break;
        }
        var gross = expectedPerItem * recipe.OutputQuantity * batch;
        var fee = gross * ListingFee;
        var net = gross - fee;
        var profit = net - cost;
        return new(cost, gross, fee, net, profit, cost > 0 ? profit / cost * 100 : null);
    }
}


