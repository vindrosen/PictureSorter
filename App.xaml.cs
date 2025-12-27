using System.Windows;

namespace PictureSorter;

/// <summary>
/// Interaction logic for App.xaml. Handles application-level exception handling.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes a new instance of the App class and sets up global exception handlers.
    /// </summary>
    public App()
    {
        // Catch unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show($"Fatal error: {ex?.Message}\n\n{ex?.StackTrace}", 
                "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };
        
        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Unhandled exception: {args.Exception.Message}\n\n{args.Exception.StackTrace}", 
                "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}
