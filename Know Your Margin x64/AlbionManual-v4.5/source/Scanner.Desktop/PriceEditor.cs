using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Scanner.Core;
namespace Scanner.Desktop;
public sealed class PriceEditor : Window
{
 public Dictionary<string,decimal> Result { get; private set; } = new();
 public PriceEditor(List<Material> materials, Dictionary<string,decimal> current)
 {
  Style=(Style)FindResource(typeof(Window)); Title="Edit saved prices"; Width=640; Height=560; WindowStartupLocation=WindowStartupLocation.CenterOwner;
  var panel=new StackPanel{Margin=new Thickness(24)}; Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
  panel.Children.Add(new TextBlock{Text="Saved manual price mode",FontSize=22,FontWeight=FontWeights.SemiBold});
  panel.Children.Add(new TextBlock{Text="Enter your unit prices and click Save prices. They remain saved until you update them. Blank fields remove the saved price. Prices are separate for each server, city and item.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,16)});
  var fields=new Dictionary<string,TextBox>();
  foreach(var m in materials){panel.Children.Add(new TextBlock{Text=m.Name});var box=new TextBox{Text=current.TryGetValue(m.ItemId,out var value)?value.ToString(CultureInfo.CurrentCulture):"",Margin=new Thickness(0,5,0,12)};System.Windows.Automation.AutomationProperties.SetName(box,m.Name+" manual unit price");fields[m.ItemId]=box;panel.Children.Add(box);}
  var actions=new StackPanel{Orientation=Orientation.Horizontal};var save=new Button{Content="Save prices",IsDefault=true};var clear=new Button{Content="Clear all"};var cancel=new Button{Content="Cancel",IsCancel=true};
  clear.Click+=(_,_)=>{foreach(var f in fields.Values)f.Text="";};
  save.Click+=(_,_)=>{var values=new Dictionary<string,decimal>();foreach(var pair in fields){if(string.IsNullOrWhiteSpace(pair.Value.Text))continue;if(!decimal.TryParse(pair.Value.Text,NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingWhite|NumberStyles.AllowTrailingWhite,CultureInfo.CurrentCulture,out var v)||v<=0||v>1000000000000m){MessageBox.Show(this,"Enter a positive unit price without thousands separators, or leave the field blank.");return;}values[pair.Key]=v;}Result=values;DialogResult=true;};
  actions.Children.Add(save);actions.Children.Add(clear);actions.Children.Add(cancel);panel.Children.Add(actions);
 }
}



