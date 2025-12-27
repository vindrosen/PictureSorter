using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PictureSorter.Services;

namespace PictureSorter.Views;

/// <summary>
/// Interaction logic for FullscreenImageWindow.xaml. Displays images in fullscreen with navigation support.
/// </summary>
public partial class FullscreenImageWindow : Window
{
    private readonly List<string> _imagePaths;
    private int _currentIndex;
    private readonly IImageService _imageService;

    /// <summary>
    /// Initializes a new instance of the FullscreenImageWindow class.
    /// </summary>
    /// <param name="imagePath">The path of the initial image to display.</param>
    /// <param name="allImagePaths">List of all image paths for navigation.</param>
    public FullscreenImageWindow(string imagePath, List<string> allImagePaths)
    {
        InitializeComponent();
        _imagePaths = allImagePaths;
        _currentIndex = _imagePaths.IndexOf(imagePath);
        _imageService = new ImageService();
        
        if (_currentIndex < 0)
        {
            _currentIndex = 0;
        }
        
        LoadCurrentImage();
        UpdateNavigationHint();
    }

    /// <summary>
    /// Loads and displays the current image based on the current index.
    /// </summary>
    private void LoadCurrentImage()
    {
        if (_currentIndex < 0 || _currentIndex >= _imagePaths.Count)
            return;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(_imagePaths[_currentIndex], UriKind.Absolute);
            bitmap.EndInit();
            
            FullImage.Source = bitmap;
        }
        catch
        {
            MessageBox.Show("Failed to load image", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    /// <summary>
    /// Updates the navigation hint text to show current position.
    /// </summary>
    private void UpdateNavigationHint()
    {
        NavigationHint.Text = $"Image {_currentIndex + 1} of {_imagePaths.Count} - Use ← → arrows to navigate, ESC or click to close";
    }

    /// <summary>
    /// Handles keyboard input for navigation (Left/Right arrows) and closing (Escape).
    /// </summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Close();
                break;
            case Key.Left:
                if (_currentIndex > 0)
                {
                    _currentIndex--;
                    LoadCurrentImage();
                    UpdateNavigationHint();
                }
                break;
            case Key.Right:
                if (_currentIndex < _imagePaths.Count - 1)
                {
                    _currentIndex++;
                    LoadCurrentImage();
                    UpdateNavigationHint();
                }
                break;
        }
    }

    /// <summary>
    /// Handles mouse click to close the fullscreen window.
    /// </summary>
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Handles the Rotate context menu item click to rotate the current image 90 degrees.
    /// </summary>
    private async void RotateImage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex < 0 || _currentIndex >= _imagePaths.Count)
            return;

        var imagePath = _imagePaths[_currentIndex];
        var success = await _imageService.RotateImageAsync(imagePath, 90);
        
        if (success)
        {
            LoadCurrentImage();
        }
        else
        {
            MessageBox.Show("Failed to rotate image", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the Delete context menu item click to delete the current image.
    /// </summary>
    private async void DeleteImage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex < 0 || _currentIndex >= _imagePaths.Count)
            return;

        var imagePath = _imagePaths[_currentIndex];
        var fileName = System.IO.Path.GetFileName(imagePath);
        
        var result = MessageBox.Show(
            $"Are you sure you want to delete this image?\n{fileName}",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var success = await _imageService.DeleteImageAsync(imagePath);
            
            if (success)
            {
                _imagePaths.RemoveAt(_currentIndex);
                
                if (_imagePaths.Count == 0)
                {
                    Close();
                    return;
                }
                
                if (_currentIndex >= _imagePaths.Count)
                {
                    _currentIndex = _imagePaths.Count - 1;
                }
                
                LoadCurrentImage();
                UpdateNavigationHint();
            }
            else
            {
                MessageBox.Show("Failed to delete image", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
