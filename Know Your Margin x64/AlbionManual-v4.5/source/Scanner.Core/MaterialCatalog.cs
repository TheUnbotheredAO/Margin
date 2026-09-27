using System.Text.RegularExpressions;
namespace Scanner.Core;
public record MaterialEntry(string ItemId,string Name,int Tier,int Enchantment);
public static class MaterialCatalog
{
 public static string GroupOf(string id){var match=Regex.Match(id,@"^T[1-8]_(WOOD|ORE|HIDE|FIBER|ROCK|PLANKS|METALBAR|LEATHER|CLOTH|STONEBLOCK)(?:_LEVEL[1-4])?(?:@[1-4])?$");if(!match.Success)return "Other Ingredients";return new[]{"WOOD","ORE","HIDE","FIBER","ROCK"}.Contains(match.Groups[1].Value)?"Raw Resources":"Refined Resources";}
 public static List<MaterialEntry> FromRecipes(IEnumerable<Recipe> recipes)=>recipes.SelectMany(r=>r.Materials)
 .Where(m=>!m.ItemId.Contains("ARTEFACT",StringComparison.OrdinalIgnoreCase)&&!m.ItemId.Contains("ARTIFACT",StringComparison.OrdinalIgnoreCase))
 .DistinctBy(m=>m.ItemId).Select(m=>new MaterialEntry(m.ItemId,m.Name,TierOf(m.ItemId),EnchantmentOf(m.ItemId))).OrderBy(m=>m.Tier).ThenBy(m=>m.Enchantment).ThenBy(m=>m.Name).ToList();
 static int TierOf(string id){var m=Regex.Match(id,@"^T([1-8])_");return m.Success?int.Parse(m.Groups[1].Value):0;}
 public static int EnchantmentOf(string id){var m=Regex.Match(id,@"@([1-4])$");return m.Success?int.Parse(m.Groups[1].Value):0;}
 public static IEnumerable<MaterialEntry> Filter(IEnumerable<MaterialEntry> entries,int tier,int enchantment,string search)=>entries.Where(m=>(tier==0||m.Tier==tier)&&(enchantment<0||m.Enchantment==enchantment)&&m.Name.Contains(search,StringComparison.OrdinalIgnoreCase));
}


