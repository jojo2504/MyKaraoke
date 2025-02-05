using System.Windows;
using MyKaraokeApp.Windows;
using MyKaraoke.Service.Logging;

namespace MyKaraokeApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {
    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        Logger.ClearLog();
        Logger.Log("Application started.");
        
        // Add global exception handling
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>{
            Logger.Fatal($"Unhandled exception: {args.ExceptionObject}");
        };

        // Pass the command-line arguments to the MainWindow
        var mainWindow = new MainWindow(e.Args);
        mainWindow.Show();
    }
}

