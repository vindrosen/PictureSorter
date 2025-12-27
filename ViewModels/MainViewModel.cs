using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using PictureSorter.Models;
using PictureSorter.Services;

namespace PictureSorter.ViewModels;

public class MainViewModel : ViewModelBase
{
    public event EventHandler? ScrollToTop;
    private readonly IImageService _imageService;
    private readonly string _stateFilePath;
    private readonly string _settingsFilePath;
    private HashSet<string> _processedImages;
    
    private string? _rootFolderPath;
    private string? _targetFolderPath;
    private string? _selectedFolderPath;
    private int _currentPage;
    private List<string> _allImageFiles;
    private string _statusMessage = string.Empty;
    private bool _isLoading;
    private int _imagesPerPage = 5;
    private int _thumbnailSize = 150;

    public ObservableCollection<FolderNode> FolderTree { get; }
    public ObservableCollection<ImageItem> CurrentPageImages { get; }

    public List<int> AvailablePageSizes { get; } = new() { 5, 10, 15, 20, 25, 50, 100 };
    public List<ThumbnailSizeOption> AvailableThumbnailSizes { get; }

    public string? RootFolderPath
    {
        get => _rootFolderPath;
        set
        {
            if (SetProperty(ref _rootFolderPath, value))
            {
                LoadFolderTree();
                SaveSettings();
            }
        }
    }

    public string? TargetFolderPath
    {
        get => _targetFolderPath;
        set
        {
            if (SetProperty(ref _targetFolderPath, value))
            {
                SaveSettings();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public int ImagesPerPage
    {
        get => _imagesPerPage;
        set
        {
            if (SetProperty(ref _imagesPerPage, value))
            {
                _currentPage = 0;
                SaveSettings();
                _ = LoadCurrentPageAsync();
            }
        }
    }

    public int ThumbnailSize
    {
        get => _thumbnailSize;
        set
        {
            if (SetProperty(ref _thumbnailSize, value))
            {
                SaveSettings();
                _ = LoadCurrentPageAsync();
            }
        }
    }

    public bool CanNavigatePrevious => _currentPage > 0;
    public bool CanNavigateNext => (_currentPage + 1) * _imagesPerPage < _allImageFiles.Count;

    public ICommand BrowseRootFolderCommand { get; }
    public ICommand BrowseTargetFolderCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand ViewFullscreenCommand { get; }
    public ICommand RotateImageCommand { get; }
    public ICommand DeleteImageCommand { get; }

    public MainViewModel() : this(new ImageService())
    {
    }

    public MainViewModel(IImageService imageService)
    {
        _imageService = imageService;
        _stateFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PictureSorter",
            "processed_images.json"
        );
        _settingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PictureSorter",
            "settings.json"
        );

        AvailableThumbnailSizes = new List<ThumbnailSizeOption>
        {
            new ThumbnailSizeOption { Name = "Small", Size = 100 },
            new ThumbnailSizeOption { Name = "Medium", Size = 150 },
            new ThumbnailSizeOption { Name = "Large", Size = 200 },
            new ThumbnailSizeOption { Name = "X-Large", Size = 250 },
            new ThumbnailSizeOption { Name = "XX-Large", Size = 300 }
        };

        FolderTree = new ObservableCollection<FolderNode>();
        CurrentPageImages = new ObservableCollection<ImageItem>();
        _allImageFiles = new List<string>();
        _processedImages = new HashSet<string>();

        BrowseRootFolderCommand = new RelayCommand(BrowseRootFolder);
        BrowseTargetFolderCommand = new RelayCommand(BrowseTargetFolder);
        PreviousPageCommand = new RelayCommand(PreviousPage, () => CanNavigatePrevious);
        NextPageCommand = new RelayCommand(NextPage, () => CanNavigateNext);
        ViewFullscreenCommand = new RelayCommand(ViewFullscreen);
        RotateImageCommand = new RelayCommand(RotateImage);
        DeleteImageCommand = new RelayCommand(DeleteImage);

        LoadProcessedImages();
        LoadSettings();
    }

    private void BrowseRootFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Root Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            RootFolderPath = dialog.FolderName;
        }
    }

    private void BrowseTargetFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Target Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            TargetFolderPath = dialog.FolderName;
            StatusMessage = $"Target folder: {dialog.FolderName}";
        }
    }

    private void LoadFolderTree()
    {
        FolderTree.Clear();
        
        if (string.IsNullOrEmpty(_rootFolderPath) || !Directory.Exists(_rootFolderPath))
            return;

        try
        {
            var rootNode = CreateFolderNode(_rootFolderPath);
            FolderTree.Add(rootNode);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading folders: {ex.Message}";
        }
    }

    private FolderNode CreateFolderNode(string folderPath)
    {
        var node = new FolderNode
        {
            Path = folderPath,
            Name = System.IO.Path.GetFileName(folderPath) ?? folderPath
        };

        try
        {
            var subfolders = Directory.GetDirectories(folderPath);
            foreach (var subfolder in subfolders)
            {
                node.Children.Add(CreateFolderNode(subfolder));
            }
        }
        catch
        {
            // Ignore access denied errors
        }

        return node;
    }

    public async Task SelectFolderAsync(string folderPath)
    {
        _selectedFolderPath = folderPath;
        _currentPage = 0;
        
        await LoadImagesForCurrentFolderAsync();
    }

    private async Task LoadImagesForCurrentFolderAsync()
    {
        if (string.IsNullOrEmpty(_selectedFolderPath))
            return;

        IsLoading = true;
        StatusMessage = "Loading images...";

        try
        {
            // Get all image files and filter out processed ones
            var allFiles = _imageService.GetImageFiles(_selectedFolderPath);
            _allImageFiles = allFiles.Where(f => !_processedImages.Contains(f)).ToList();

            await LoadCurrentPageAsync();
            
            StatusMessage = $"Loaded {_allImageFiles.Count} images from folder";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading images: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCurrentPageAsync()
    {
        CurrentPageImages.Clear();

        var pageFiles = _allImageFiles
            .Skip(_currentPage * _imagesPerPage)
            .Take(_imagesPerPage)
            .ToList();

        foreach (var filePath in pageFiles)
        {
            var imageItem = new ImageItem
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                CreationDate = File.GetCreationTime(filePath)
            };

            // Subscribe to property changes
            imageItem.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(ImageItem.IsChecked) && imageItem.IsChecked)
                {
                    await OnImageCheckedAsync(imageItem);
                }
            };

            CurrentPageImages.Add(imageItem);

            // Load thumbnail asynchronously with configured size
            var thumbnail = await _imageService.LoadThumbnailAsync(filePath, _thumbnailSize);
            imageItem.Thumbnail = thumbnail;
        }

        OnPropertyChanged(nameof(CanNavigatePrevious));
        OnPropertyChanged(nameof(CanNavigateNext));
    }

    private async Task OnImageCheckedAsync(ImageItem imageItem)
    {
        if (string.IsNullOrEmpty(TargetFolderPath))
        {
            StatusMessage = "Please select a target folder first";
            imageItem.IsChecked = false;
            return;
        }

        try
        {
            var success = await _imageService.CopyImageAsync(imageItem.FilePath, TargetFolderPath);
            
            if (success)
            {
                // Mark as processed
                _processedImages.Add(imageItem.FilePath);
                SaveProcessedImages();

                // Remove from current view
                _allImageFiles.Remove(imageItem.FilePath);
                
                StatusMessage = $"Copied: {imageItem.FileName}";

                // Reload current page to refill
                await LoadCurrentPageAsync();
            }
            else
            {
                StatusMessage = $"Skipped (already exists): {imageItem.FileName}";
                imageItem.IsChecked = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error copying {imageItem.FileName}: {ex.Message}";
            imageItem.IsChecked = false;
        }
    }

    private void PreviousPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            _ = LoadCurrentPageAsync();
            ScrollToTop?.Invoke(this, EventArgs.Empty);
        }
    }

    private void NextPage()
    {
        if ((_currentPage + 1) * _imagesPerPage < _allImageFiles.Count)
        {
            _currentPage++;
            _ = LoadCurrentPageAsync();
            ScrollToTop?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ViewFullscreen(object? parameter)
    {
        if (parameter is string imagePath)
        {
            var allPaths = CurrentPageImages.Select(img => img.FilePath).ToList();
            var fullscreenWindow = new PictureSorter.Views.FullscreenImageWindow(imagePath, allPaths);
            fullscreenWindow.ShowDialog();
        }
    }

    private async void RotateImage(object? parameter)
    {
        if (parameter is string imagePath)
        {
            StatusMessage = "Rotating image...";
            var success = await _imageService.RotateImageAsync(imagePath, 90);
            
            if (success)
            {
                StatusMessage = "Image rotated successfully";
                // Reload the current page to show the rotated image
                await LoadCurrentPageAsync();
            }
            else
            {
                StatusMessage = "Failed to rotate image";
            }
        }
    }

    private async void DeleteImage(object? parameter)
    {
        if (parameter is string imagePath)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete this image?\n{Path.GetFileName(imagePath)}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                StatusMessage = "Deleting image...";
                var success = await _imageService.DeleteImageAsync(imagePath);
                
                if (success)
                {
                    // Remove from all lists
                    _allImageFiles.Remove(imagePath);
                    _processedImages.Add(imagePath);
                    SaveProcessedImages();
                    
                    StatusMessage = $"Deleted: {Path.GetFileName(imagePath)}";
                    
                    // Reload the current page
                    await LoadCurrentPageAsync();
                }
                else
                {
                    StatusMessage = "Failed to delete image";
                }
            }
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    _imagesPerPage = settings.ImagesPerPage;
                    _thumbnailSize = settings.ThumbnailSize;
                    
                    if (!string.IsNullOrEmpty(settings.RootFolderPath) && Directory.Exists(settings.RootFolderPath))
                    {
                        _rootFolderPath = settings.RootFolderPath;
                        LoadFolderTree();
                    }
                    
                    if (!string.IsNullOrEmpty(settings.TargetFolderPath) && Directory.Exists(settings.TargetFolderPath))
                    {
                        _targetFolderPath = settings.TargetFolderPath;
                    }
                    
                    OnPropertyChanged(nameof(ImagesPerPage));
                    OnPropertyChanged(nameof(ThumbnailSize));
                    OnPropertyChanged(nameof(RootFolderPath));
                    OnPropertyChanged(nameof(TargetFolderPath));
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading settings: {ex.Message}";
        }
    }

    private void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var settings = new AppSettings
            {
                ImagesPerPage = _imagesPerPage,
                ThumbnailSize = _thumbnailSize,
                RootFolderPath = _rootFolderPath,
                TargetFolderPath = _targetFolderPath
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving settings: {ex.Message}";
        }
    }

    private void LoadProcessedImages()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                var json = File.ReadAllText(_stateFilePath);
                var list = JsonSerializer.Deserialize<List<string>>(json);
                _processedImages = list?.ToHashSet() ?? new HashSet<string>();
                StatusMessage = $"Loaded {_processedImages.Count} processed images from previous sessions";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading state: {ex.Message}";
        }
    }

    private void SaveProcessedImages()
    {
        try
        {
            var directory = Path.GetDirectoryName(_stateFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_processedImages.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_stateFilePath, json);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving state: {ex.Message}";
        }
    }
}

public class ThumbnailSizeOption
{
    public string Name { get; set; } = string.Empty;
    public int Size { get; set; }
}

public class AppSettings
{
    public int ImagesPerPage { get; set; } = 5;
    public int ThumbnailSize { get; set; } = 150;
    public string? RootFolderPath { get; set; }
    public string? TargetFolderPath { get; set; }
}
