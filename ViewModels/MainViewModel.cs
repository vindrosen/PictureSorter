using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using PictureSorter.Models;
using PictureSorter.Services;

namespace PictureSorter.ViewModels;

public enum SortOption
{
    CreationDate,
    FileName,
    FileSize
}

public class MainViewModel : ViewModelBase
{
    public event EventHandler? ScrollToTop;
    private readonly IImageService _imageService;
    private readonly string _stateFilePath;
    private readonly string _settingsFilePath;
    private readonly string _profilesDirectory;
    private HashSet<string> _processedImages;
    private HashSet<string> _selectedImages;
    private CancellationTokenSource? _loadFolderCancellation;
    private string? _currentProfileName;
    private string? _selectedProfile;
    
    private string? _rootFolderPath;
    private string? _targetFolderPath;
    private string? _selectedFolderPath;
    private int _currentPage;
    private List<string> _allImageFiles;
    private string _statusMessage = string.Empty;
    private bool _isLoading;
    private int _imagesPerPage = 5;
    private int _thumbnailSize = 150;
    private SortOption _sortBy = SortOption.CreationDate;
    private bool _sortDescending = true;

    public ObservableCollection<FolderNode> FolderTree { get; }
    public ObservableCollection<ImageItem> CurrentPageImages { get; }
    public ObservableCollection<string> AvailableProfiles { get; }

    public string? CurrentProfileName
    {
        get => _currentProfileName;
        set => SetProperty(ref _currentProfileName, value);
    }

    public string? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetProperty(ref _selectedProfile, value) && !string.IsNullOrEmpty(value))
            {
                LoadProfile(value);
            }
        }
    }

    public List<int> AvailablePageSizes { get; } = new() { 5, 10, 15, 20, 25, 50, 100 };
    public List<ThumbnailSizeOption> AvailableThumbnailSizes { get; }
    public List<SortOption> AvailableSortOptions { get; } = new() { SortOption.CreationDate, SortOption.FileName, SortOption.FileSize };

    public SortOption SortBy
    {
        get => _sortBy;
        set
        {
            if (_sortBy != value)
            {
                _sortBy = value;
                OnPropertyChanged();
                _ = ApplySortAndReloadAsync();
            }
        }
    }

    public bool SortDescending
    {
        get => _sortDescending;
        set
        {
            if (_sortDescending != value)
            {
                _sortDescending = value;
                OnPropertyChanged();
                _ = ApplySortAndReloadAsync();
            }
        }
    }

    public string? RootFolderPath
    {
        get => _rootFolderPath;
        set
        {
            if (SetProperty(ref _rootFolderPath, value))
            {
                LoadFolderTree();
                SaveSettings();
                AutoSaveCurrentProfile();
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
                AutoSaveCurrentProfile();
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

    public int SelectedImagesCount => _selectedImages?.Count ?? 0;
    public bool HasSelectedImages => SelectedImagesCount > 0;

    public ICommand BrowseRootFolderCommand { get; }
    public ICommand BrowseTargetFolderCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand ViewFullscreenCommand { get; }
    public ICommand RotateClockwiseCommand { get; }
    public ICommand RotateCounterClockwiseCommand { get; }
    public ICommand DeleteImageCommand { get; }
    public ICommand OpenLocationCommand { get; }
    public ICommand CopySelectedCommand { get; }
    public ICommand MoveImageCommand { get; }
    public ICommand ToggleSortDirectionCommand { get; }
    public ICommand CreateProfileCommand { get; }
    public ICommand LoadProfileCommand { get; }
    public ICommand SaveCurrentProfileCommand { get; }
    public ICommand DeleteProfileCommand { get; }

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
        _profilesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PictureSorter",
            "Profiles"
        );

        // Ensure profiles directory exists
        Directory.CreateDirectory(_profilesDirectory);

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
        AvailableProfiles = new ObservableCollection<string>();
        _allImageFiles = new List<string>();
        _processedImages = new HashSet<string>();
        _selectedImages = new HashSet<string>();

        BrowseRootFolderCommand = new RelayCommand(BrowseRootFolder);
        BrowseTargetFolderCommand = new RelayCommand(BrowseTargetFolder);
        PreviousPageCommand = new RelayCommand(PreviousPage, () => CanNavigatePrevious);
        NextPageCommand = new RelayCommand(NextPage, () => CanNavigateNext);
        ViewFullscreenCommand = new RelayCommand(ViewFullscreen);
        RotateClockwiseCommand = new RelayCommand(RotateClockwise);
        RotateCounterClockwiseCommand = new RelayCommand(RotateCounterClockwise);
        DeleteImageCommand = new RelayCommand(DeleteImage);
        OpenLocationCommand = new RelayCommand(OpenLocation);
        CopySelectedCommand = new RelayCommand(CopySelected, () => HasSelectedImages);
        MoveImageCommand = new RelayCommand(MoveImage);
        ToggleSortDirectionCommand = new RelayCommand(ToggleSortDirection);
        CreateProfileCommand = new RelayCommand(CreateProfile);
        LoadProfileCommand = new RelayCommand(LoadProfile);
        SaveCurrentProfileCommand = new RelayCommand(SaveCurrentProfile);
        DeleteProfileCommand = new RelayCommand(DeleteProfile);
        ToggleSortDirectionCommand = new RelayCommand(ToggleSortDirection);

        LoadProcessedImages();
        LoadSettings();
        LoadAvailableProfiles();
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
        // Cancel any ongoing folder load operation
        _loadFolderCancellation?.Cancel();
        _loadFolderCancellation = new CancellationTokenSource();
        
        _selectedFolderPath = folderPath;
        _currentPage = 0;
        
        await LoadImagesForCurrentFolderAsync(_loadFolderCancellation.Token);
    }

    private async Task LoadImagesForCurrentFolderAsync(CancellationToken cancellationToken = default)
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
            
            // Check if cancelled before continuing
            if (cancellationToken.IsCancellationRequested)
                return;
            
            ApplySort();

            await LoadCurrentPageAsync(cancellationToken);
            
            // Check if cancelled before updating UI
            if (cancellationToken.IsCancellationRequested)
                return;
            
            // Update navigation properties
            OnPropertyChanged(nameof(CanNavigatePrevious));
            OnPropertyChanged(nameof(CanNavigateNext));
            
            // Scroll to top when loading new folder
            ScrollToTop?.Invoke(this, EventArgs.Empty);
            
            StatusMessage = $"Loaded {_allImageFiles.Count} images from folder";
        }
        catch (OperationCanceledException)
        {
            // Expected when folder changes quickly
            StatusMessage = "Loading cancelled";
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

    private async Task LoadCurrentPageAsync(CancellationToken cancellationToken = default)
    {
        CurrentPageImages.Clear();

        var pageFiles = _allImageFiles
            .Skip(_currentPage * _imagesPerPage)
            .Take(_imagesPerPage)
            .ToList();

        foreach (var filePath in pageFiles)
        {
            // Check if operation was cancelled
            if (cancellationToken.IsCancellationRequested)
                return;
            
            var imageItem = new ImageItem
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                CreationDate = File.GetCreationTime(filePath),
                IsChecked = _selectedImages.Contains(filePath)
            };

            // Subscribe to property changes
            imageItem.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ImageItem.IsChecked))
                {
                    if (imageItem.IsChecked)
                    {
                        _selectedImages.Add(imageItem.FilePath);
                    }
                    else
                    {
                        _selectedImages.Remove(imageItem.FilePath);
                    }
                    OnPropertyChanged(nameof(SelectedImagesCount));
                    OnPropertyChanged(nameof(HasSelectedImages));
                    AutoSaveCurrentProfile();
                }
            };

            CurrentPageImages.Add(imageItem);

            // Load thumbnail asynchronously with configured size
            var thumbnail = await _imageService.LoadThumbnailAsync(filePath, _thumbnailSize);
            
            // Check again after async operation
            if (cancellationToken.IsCancellationRequested)
                return;
                
            imageItem.Thumbnail = thumbnail;
            
            // Load GPS coordinates asynchronously
            var gpsData = await _imageService.GetGpsCoordinatesAsync(filePath);
            
            // Check again after async operation
            if (cancellationToken.IsCancellationRequested)
                return;
                
            if (gpsData.HasValue)
            {
                imageItem.Latitude = gpsData.Value.Latitude;
                imageItem.Longitude = gpsData.Value.Longitude;
            }
        }

        OnPropertyChanged(nameof(CanNavigatePrevious));
        OnPropertyChanged(nameof(CanNavigateNext));
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

    private async void RotateClockwise(object? parameter)
    {
        await RotateImageAsync(parameter, 90);
    }

    private async void RotateCounterClockwise(object? parameter)
    {
        await RotateImageAsync(parameter, -90);
    }

    private async Task RotateImageAsync(object? parameter, int degrees)
    {
        if (parameter is string imagePath)
        {
            StatusMessage = "Rotating image...";
            var success = await _imageService.RotateImageAsync(imagePath, degrees);
            
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

    private void ApplySort()
    {
        switch (_sortBy)
        {
            case SortOption.CreationDate:
                _allImageFiles = _sortDescending
                    ? _allImageFiles.OrderByDescending(f => File.GetCreationTime(f)).ToList()
                    : _allImageFiles.OrderBy(f => File.GetCreationTime(f)).ToList();
                break;
            case SortOption.FileName:
                _allImageFiles = _sortDescending
                    ? _allImageFiles.OrderByDescending(f => Path.GetFileName(f)).ToList()
                    : _allImageFiles.OrderBy(f => Path.GetFileName(f)).ToList();
                break;
            case SortOption.FileSize:
                _allImageFiles = _sortDescending
                    ? _allImageFiles.OrderByDescending(f => new FileInfo(f).Length).ToList()
                    : _allImageFiles.OrderBy(f => new FileInfo(f).Length).ToList();
                break;
        }
    }

    private async Task ApplySortAndReloadAsync()
    {
        if (_allImageFiles?.Any() != true)
            return;
            
        ApplySort();
        _currentPage = 0;
        await LoadCurrentPageAsync();
        OnPropertyChanged(nameof(CanNavigatePrevious));
        OnPropertyChanged(nameof(CanNavigateNext));
    }

    private void ToggleSortDirection(object? parameter)
    {
        SortDescending = !SortDescending;
    }

    private async void MoveImage(object? parameter)
    {
        if (parameter is not string filePath)
            return;

        if (string.IsNullOrEmpty(TargetFolderPath))
        {
            StatusMessage = "Please select a target folder first";
            return;
        }

        IsLoading = true;
        try
        {
            var success = await _imageService.MoveImageAsync(filePath, TargetFolderPath);
            
            if (success)
            {
                // Mark as processed
                _processedImages.Add(filePath);
                SaveProcessedImages();
                
                // Remove from selection if present
                _selectedImages.Remove(filePath);
                OnPropertyChanged(nameof(SelectedImagesCount));
                OnPropertyChanged(nameof(HasSelectedImages));
                
                // Remove from current view
                _allImageFiles.Remove(filePath);
                
                // Auto-save profile
                AutoSaveCurrentProfile();
                
                StatusMessage = $"Moved: {Path.GetFileName(filePath)}";
                
                // Reload current page to refill
                await LoadCurrentPageAsync();
            }
            else
            {
                StatusMessage = $"File already exists in target folder";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error moving image: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
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
                    
                    // Auto-save profile
                    AutoSaveCurrentProfile();
                    
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

    private void OpenLocation(object? parameter)
    {
        if (parameter is not ImageItem imageItem || !imageItem.HasGpsData)
        {
            StatusMessage = "No GPS data available";
            return;
        }

        try
        {
            // Use invariant culture to ensure period as decimal separator
            var lat = imageItem.Latitude?.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            var lon = imageItem.Longitude?.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            var url = $"https://www.google.com/maps?q={lat},{lon}";
            
            StatusMessage = $"Opening: {url}";
            
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
            
            StatusMessage = $"Opened location: {lat}, {lon}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening location: {ex.Message}";
            System.Windows.MessageBox.Show($"Failed to open browser:\n{ex.Message}\n\nURL: https://www.google.com/maps?q={imageItem.Latitude},{imageItem.Longitude}", "Error");
        }
    }
    private async void CopySelected()
    {
        if (string.IsNullOrEmpty(TargetFolderPath))
        {
            StatusMessage = "Please select a target folder first";
            return;
        }

        if (!_selectedImages.Any())
        {
            StatusMessage = "No images selected";
            return;
        }

        IsLoading = true;
        int successCount = 0;
        int skipCount = 0;
        int errorCount = 0;

        try
        {
            // Create a copy of selected images to iterate over
            var imagesToCopy = _selectedImages.ToList();
            
            foreach (var filePath in imagesToCopy)
            {
                try
                {
                    var success = await _imageService.CopyImageAsync(filePath, TargetFolderPath);
                    
                    if (success)
                    {
                        // Mark as processed
                        _processedImages.Add(filePath);
                        
                        // Remove from selection
                        _selectedImages.Remove(filePath);
                        
                        // Remove from current view if present
                        _allImageFiles.Remove(filePath);
                        successCount++;
                    }
                    else
                    {
                        skipCount++;
                    }
                }
                catch
                {
                    errorCount++;
                }
            }

            // Save processed images
            SaveProcessedImages();

            // Update counts
            OnPropertyChanged(nameof(SelectedImagesCount));
            OnPropertyChanged(nameof(HasSelectedImages));
            
            // Auto-save profile
            AutoSaveCurrentProfile();

            // Show summary
            StatusMessage = $"Copied: {successCount}, Skipped: {skipCount}, Errors: {errorCount}";

            // Reload current page to refill
            await LoadCurrentPageAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Profile Management Methods
    
    private void LoadAvailableProfiles()
    {
        AvailableProfiles.Clear();
        
        if (!Directory.Exists(_profilesDirectory))
            return;
            
        var profileFiles = Directory.GetFiles(_profilesDirectory, "*.json");
        foreach (var file in profileFiles)
        {
            var profileName = Path.GetFileNameWithoutExtension(file);
            AvailableProfiles.Add(profileName);
        }
    }

    private void CreateProfile(object? parameter)
    {
        var profileName = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter profile name:",
            "Create New Profile",
            "New Profile"
        );

        if (string.IsNullOrWhiteSpace(profileName))
            return;

        // Check if profile already exists
        var profilePath = Path.Combine(_profilesDirectory, $"{profileName}.json");
        if (File.Exists(profilePath))
        {
            var result = MessageBox.Show(
                $"Profile '{profileName}' already exists. Overwrite?",
                "Profile Exists",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
                return;
        }

        var profile = new Profile
        {
            Name = profileName,
            RootFolderPath = null,
            TargetFolderPath = null,
            ProcessedImages = new HashSet<string>(),
            SelectedImages = new HashSet<string>(),
            ImagesPerPage = _imagesPerPage,
            ThumbnailSize = _thumbnailSize,
            SortBy = _sortBy.ToString(),
            SortDescending = _sortDescending,
            CreatedDate = DateTime.Now,
            LastModified = DateTime.Now
        };

        SaveProfile(profile);
        CurrentProfileName = profileName;
        
        // Clear current state
        _rootFolderPath = null;
        _targetFolderPath = null;
        _processedImages.Clear();
        _selectedImages.Clear();
        _allImageFiles.Clear();
        CurrentPageImages.Clear();
        FolderTree.Clear();
        
        // Notify property changes
        OnPropertyChanged(nameof(RootFolderPath));
        OnPropertyChanged(nameof(TargetFolderPath));
        OnPropertyChanged(nameof(SelectedImagesCount));
        OnPropertyChanged(nameof(HasSelectedImages));
        
        LoadAvailableProfiles();
        StatusMessage = $"Profile '{profileName}' created successfully";
    }

    private void LoadProfile(object? parameter)
    {
        string? profileName = parameter as string;
        
        // If no parameter, use the selected profile from dropdown
        if (string.IsNullOrEmpty(profileName))
        {
            profileName = _selectedProfile;
        }
        
        if (string.IsNullOrEmpty(profileName))
        {
            MessageBox.Show("Please select a profile to load.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var profilePath = Path.Combine(_profilesDirectory, $"{profileName}.json");
        if (!File.Exists(profilePath))
        {
            MessageBox.Show($"Profile '{profileName}' not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            var json = File.ReadAllText(profilePath);
            var profile = JsonSerializer.Deserialize<Profile>(json);

            if (profile == null)
            {
                MessageBox.Show("Failed to load profile.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Apply profile settings
            _rootFolderPath = profile.RootFolderPath;
            _targetFolderPath = profile.TargetFolderPath;
            _processedImages = new HashSet<string>(profile.ProcessedImages);
            _selectedImages = new HashSet<string>(profile.SelectedImages);
            _imagesPerPage = profile.ImagesPerPage;
            _thumbnailSize = profile.ThumbnailSize;
            _sortDescending = profile.SortDescending;
            
            if (Enum.TryParse<SortOption>(profile.SortBy, out var sortOption))
            {
                _sortBy = sortOption;
            }

            CurrentProfileName = profileName;

            // Notify all property changes
            OnPropertyChanged(nameof(RootFolderPath));
            OnPropertyChanged(nameof(TargetFolderPath));
            OnPropertyChanged(nameof(ImagesPerPage));
            OnPropertyChanged(nameof(ThumbnailSize));
            OnPropertyChanged(nameof(SortBy));
            OnPropertyChanged(nameof(SortDescending));
            OnPropertyChanged(nameof(SelectedImagesCount));
            OnPropertyChanged(nameof(HasSelectedImages));

            // Reload folder tree and images
            LoadFolderTree();
            if (!string.IsNullOrEmpty(_selectedFolderPath))
            {
                _ = LoadImagesForCurrentFolderAsync();
            }

            StatusMessage = $"Profile '{profileName}' loaded successfully";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveCurrentProfile(object? parameter)
    {
        if (string.IsNullOrEmpty(_currentProfileName))
        {
            MessageBox.Show("No profile selected. Create a new profile first.", "No Profile", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var profile = new Profile
        {
            Name = _currentProfileName,
            RootFolderPath = _rootFolderPath,
            TargetFolderPath = _targetFolderPath,
            ProcessedImages = new HashSet<string>(_processedImages),
            SelectedImages = new HashSet<string>(_selectedImages),
            ImagesPerPage = _imagesPerPage,
            ThumbnailSize = _thumbnailSize,
            SortBy = _sortBy.ToString(),
            SortDescending = _sortDescending,
            LastModified = DateTime.Now
        };

        // Preserve creation date if profile exists
        var profilePath = Path.Combine(_profilesDirectory, $"{_currentProfileName}.json");
        if (File.Exists(profilePath))
        {
            try
            {
                var json = File.ReadAllText(profilePath);
                var existingProfile = JsonSerializer.Deserialize<Profile>(json);
                if (existingProfile != null)
                {
                    profile.CreatedDate = existingProfile.CreatedDate;
                }
            }
            catch { }
        }

        SaveProfile(profile);
        StatusMessage = $"Profile '{_currentProfileName}' saved successfully";
    }

    private void DeleteProfile(object? parameter)
    {
        if (parameter is not string profileName)
        {
            MessageBox.Show("Please select a profile to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Are you sure you want to delete profile '{profileName}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.No)
            return;

        var profilePath = Path.Combine(_profilesDirectory, $"{profileName}.json");
        if (File.Exists(profilePath))
        {
            try
            {
                File.Delete(profilePath);
                LoadAvailableProfiles();
                
                if (_currentProfileName == profileName)
                {
                    CurrentProfileName = null;
                }

                StatusMessage = $"Profile '{profileName}' deleted successfully";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void SaveProfile(Profile profile)
    {
        try
        {
            Directory.CreateDirectory(_profilesDirectory);

            var profilePath = Path.Combine(_profilesDirectory, $"{profile.Name}.json");
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(profilePath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void AutoSaveCurrentProfile()
    {
        if (string.IsNullOrEmpty(_currentProfileName))
            return;
            
        var profile = new Profile
        {
            Name = _currentProfileName,
            RootFolderPath = _rootFolderPath,
            TargetFolderPath = _targetFolderPath,
            ProcessedImages = new HashSet<string>(_processedImages),
            SelectedImages = new HashSet<string>(_selectedImages),
            ImagesPerPage = _imagesPerPage,
            ThumbnailSize = _thumbnailSize,
            SortBy = _sortBy.ToString(),
            SortDescending = _sortDescending,
            LastModified = DateTime.Now
        };

        // Preserve creation date if profile exists
        var profilePath = Path.Combine(_profilesDirectory, $"{_currentProfileName}.json");
        if (File.Exists(profilePath))
        {
            try
            {
                var json = File.ReadAllText(profilePath);
                var existingProfile = JsonSerializer.Deserialize<Profile>(json);
                if (existingProfile != null)
                {
                    profile.CreatedDate = existingProfile.CreatedDate;
                }
            }
            catch { }
        }

        SaveProfile(profile);
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
