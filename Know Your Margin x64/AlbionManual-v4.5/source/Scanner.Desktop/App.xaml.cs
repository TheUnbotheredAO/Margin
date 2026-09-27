using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
namespace Scanner.Desktop;
public partial class App : Application
{
    internal static void Log(string text)
    {
        try { File.AppendAllText(Path.Combine(AppContext.BaseDirectory,"startup.log"),DateTimeOffset.Now.ToString("O")+" "+text+Environment.NewLine); } catch { }
    }
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Log("Startup entered; version 4.5.0");
        DispatcherUnhandledException += (_,args) => { Log(args.Exception.ToString()); MessageBox.Show(args.Exception.Message,"Albion Manual Crafting error"); args.Handled=true; Shutdown(1); };
        var loading=new Window { Title="Albion Manual Crafting — starting", Width=460, Height=180, WindowStartupLocation=WindowStartupLocation.CenterScreen,
            Content=new TextBlock { Text="Loading your recipes and saved prices…", Margin=new Thickness(30), FontSize=18, TextWrapping=TextWrapping.Wrap, VerticalAlignment=VerticalAlignment.Center } };
        MainWindow=loading; loading.Show(); loading.Activate();
        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(() =>
        {
            try
            {
                Log("Loading main window");
                var window=new MainWindow(); MainWindow=window;
                Log("Main window constructed");
                window.Show(); window.WindowState=WindowState.Normal; window.Activate(); loading.Close();
                Log("Main window shown");
                if(e.Args.Contains("--smoke-test")) { window.SmokeTest(); Log("Smoke test passed"); window.Close(); Shutdown(0); }
            }
            catch(Exception ex) { Log(ex.ToString()); MessageBox.Show(loading,"Could not open the app.\n\n"+ex.Message+"\n\nDetails are in startup.log beside the executable.","Albion startup error");Shutdown(1); }
        }));
    }
}











