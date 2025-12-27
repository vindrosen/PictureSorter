using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PictureSorter.Models;
using PictureSorter.ViewModels;

namespace PictureSorter.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml. The main application window.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the MainWindow class and sets up event handlers.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        
        // Subscribe to ScrollToTop event
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ScrollToTop += (s, e) => ImageScrollViewer.ScrollToTop();
        }
        
        DataContextChanged += (s, e) =>
        {
            if (e.NewValue is MainViewModel vm)
            {
                vm.ScrollToTop += (sender, args) => ImageScrollViewer.ScrollToTop();
            }
        };
    }

    /// <summary>
    /// Handles the TreeView SelectedItemChanged event to load images from the selected folder.
    /// </summary>
    private async void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is FolderNode folderNode && DataContext is MainViewModel viewModel)
        {
            await viewModel.SelectFolderAsync(folderNode.Path);
        }
    }

    /// <summary>
    /// Handles the Image MouseLeftButtonDown event to open the fullscreen viewer.
    /// </summary>
    private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Image image && image.Tag is string imagePath && DataContext is MainViewModel viewModel)
        {
            var allPaths = viewModel.CurrentPageImages.Select(img => img.FilePath).ToList();
            var fullscreenWindow = new FullscreenImageWindow(imagePath, allPaths);
            fullscreenWindow.ShowDialog();
        }
    }
}
