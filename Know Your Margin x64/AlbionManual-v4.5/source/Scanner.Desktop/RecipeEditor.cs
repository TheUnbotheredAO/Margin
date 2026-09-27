using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Scanner.Core;
namespace Scanner.Desktop;
public sealed class RecipeEditor:Window
{
 public Recipe? Result{get;private set;}
 public RecipeEditor(Recipe? recipe,IEnumerable<Recipe> catalog)
 {
  Style=(Style)FindResource(typeof(Window));Title=recipe==null?"New crafting recipe":"Edit crafting recipe";Width=820;Height=700;WindowStartupLocation=WindowStartupLocation.CenterOwner;
  var all=catalog.ToList();var known=all.SelectMany(r=>r.Materials).Concat(all.Select(r=>new Material(r.ItemId,r.Name,1))).DistinctBy(m=>m.ItemId).OrderBy(m=>m.Name).ToList();
  var panel=new StackPanel{Margin=new Thickness(24)};Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
  panel.Children.Add(new TextBlock{Text=Title,FontSize=24,Margin=new Thickness(0,0,0,16)});
  TextBox Field(string title,string value){panel.Children.Add(new TextBlock{Text=title});var box=new TextBox{Text=value,Margin=new Thickness(0,4,0,12)};panel.Children.Add(box);return box;}
  var name=Field("Item name",recipe?.Name??"");var category=Field("Category",recipe?.Category??"Custom");var tier=Field("Tier (1–8)",recipe?.Tier.ToString()??"3");var output=Field("Output items per craft",recipe?.OutputQuantity.ToString()??"1");
  panel.Children.Add(new TextBlock{Text="Materials · select by name",FontSize=18,Margin=new Thickness(0,8,0,10)});
  var rows=new List<(ComboBox Picker,TextBox Qty)>();
  for(int i=0;i<Math.Max(5,recipe?.Materials.Count??0);i++){var grid=new Grid{Margin=new Thickness(0,0,0,8)};grid.ColumnDefinitions.Add(new(){Width=new GridLength(3,GridUnitType.Star)});grid.ColumnDefinitions.Add(new());var picker=new ComboBox{ItemsSource=new[]{new Material("","— Unused —",0)}.Concat(known),DisplayMemberPath="Name",IsTextSearchEnabled=true,Margin=new Thickness(0,0,10,0)};picker.SelectedIndex=0;var qty=new TextBox();if(i<(recipe?.Materials.Count??0)){var m=recipe!.Materials[i];picker.SelectedItem=known.First(x=>x.ItemId==m.ItemId);qty.Text=m.Quantity.ToString(CultureInfo.CurrentCulture);}Grid.SetColumn(qty,1);grid.Children.Add(picker);grid.Children.Add(qty);panel.Children.Add(grid);rows.Add((picker,qty));}
  panel.Children.Add(new TextBlock{Text="Choose unused for empty rows. Item identifiers are kept internally. New custom item names receive an internal identifier automatically.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,10)});
  var save=new Button{Content="Save recipe",IsDefault=true};save.Click+=(_,_)=>{try{var materials=rows.Where(x=>x.Picker.SelectedItem is Material m && m.ItemId!="").Select(x=>{var m=(Material)x.Picker.SelectedItem;return new Material(m.ItemId,m.Name,decimal.Parse(x.Qty.Text,CultureInfo.CurrentCulture));}).ToList();var itemId=recipe?.ItemId??all.FirstOrDefault(r=>string.Equals(r.Name,name.Text.Trim(),StringComparison.OrdinalIgnoreCase))?.ItemId??"CUSTOM_"+Guid.NewGuid().ToString("N").ToUpperInvariant();var result=new Recipe(itemId,name.Text.Trim(),category.Text.Trim(),int.Parse(tier.Text),int.Parse(output.Text),materials,"User-edited recipe",recipe?.Variant??1,recipe?.SilverCost??0);DataFiles.Validate(result);Result=result;DialogResult=true;}catch(Exception ex){MessageBox.Show(this,"Check the recipe name, tier, output and material quantities. "+ex.Message);}};panel.Children.Add(save);
 }
}
