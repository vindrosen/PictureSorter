# Picture Sorter

A Windows desktop application for efficiently sorting and managing photos with an intuitive interface and powerful features.

The application is designed for **manual photo review workflows**, where users can quickly browse, preview, and organize their image collections.

**Creator:** Robert Erlandsson

**Project Statistics:**
- **Total Lines of Code:** 1,389
- **C# Code:** 1,138 lines
- **XAML UI:** 251 lines
- **Source Files:** 13

------------------------------------------------------------------------

## ✨ Features

### Core Functionality
-   Runs locally on **Windows 11**
-   Built with **C# / .NET 8 / WPF**
-   Browse folders and subfolders on the local file system
-   **Configurable pagination** (5, 10, 15, 20, 25, 50, or 100 images per page)
-   Navigate forward and backward between pages with automatic scroll-to-top
-   Copy images instantly using a checkbox
-   Automatically replaces processed images
-   Optimized thumbnail loading with **EXIF orientation support**
-   MVVM-based architecture

### Advanced Features
-   **Fullscreen Image Viewer**
    -   Click any image to view it fullscreen
    -   Navigate with arrow keys (Left/Right)
    -   Right-click menu for rotate and delete
-   **Image Manipulation**
    -   Rotate images 90° clockwise or counter-clockwise
    -   Delete unwanted images
    -   Operations work in both grid and fullscreen views
-   **GPS Location Support**
    -   Automatically reads GPS coordinates from EXIF metadata
    -   Displays coordinates under images with location data
    -   Click coordinates or right-click → "📍 View Location" to open in Google Maps
    -   Supports UInt64 packed rational format for GPS data
-   **Configurable Thumbnail Sizes**
    -   5 size options: Small (100px) to X-Large (300px)
-   **Persistent Settings**
    -   Remembers root and target folders
    -   Saves pagination and thumbnail size preferences
    -   Tracks processed images across sessions
-   **Image Metadata Display**
    -   Shows creation date for each photo
    -   Displays GPS coordinates when available
    -   File name display
-   **Custom Application Icon**

------------------------------------------------------------------------

## 🧭 Application Overview

    ----------------------------------------------------------
    | Root: [Browse]  Target: [Browse]                      |
    | Images/page: [dropdown]  Size: [dropdown]             |
    |--------------------------------------------------------|
    | Folder Tree      | Image Grid (configurable)          |
    |                  |--------------------------------------|
    |  📁 2024         | [img]  [img]  [img]  [img]  [img]   |
    |    📁 Vacation   |  ☑      ☐      ☐      ☐      ☐     |
    |    📁 Birthday   | Date   Date   Date   Date   Date    |
    |                  |                                      |
    |                  | [img]  [img]  [img]  [img]  [img]   |
    |                  |  ☐      ☐      ☐      ☐      ☐     |
    |                  | Date   Date   Date   Date   Date    |
    |                  |--------------------------------------|
    |                  | ← Previous  Page 1 of 10   Next →   |
    ----------------------------------------------------------

### Features in Action
- Click any image → Opens fullscreen viewer
- Right-click image → Rotate or Delete menu
- Check checkbox → Copies to target folder instantly
- Scrollbar resets to top on page navigation

------------------------------------------------------------------------

## 📁 Folder Navigation

-   A folder tree is shown on the left side
-   Displays all subfolders of a selected root folder
-   Clicking a folder:
    -   Loads images from that folder
    -   Resets pagination to page 1
-   Navigation up and down the folder hierarchy is supported

------------------------------------------------------------------------

## 🖼 Image Display Rules

-   **Configurable images per page**: 5, 10, 15, 20, 25, 50, or 100
-   **Configurable thumbnail sizes**: Small (100px) to X-Large (300px)
-   Only images from the selected folder are shown
-   Images already copied are excluded
-   **EXIF orientation is automatically applied** to match File Explorer
-   Supported formats:
    -   `.jpg` / `.jpeg`
    -   `.png`
    -   `.bmp`
    -   `.webp`

------------------------------------------------------------------------

## 📄 Pagination

Flexible paging system:

-   Page sizes: `5, 10, 15, 20, 25, 50, 100`
-   User-configurable via dropdown
-   Automatic scroll-to-top on page change

-   Implementation:

    ``` csharp
    Skip(currentPage * pageSize)
    Take(pageSize)
    ```

Navigation: 
- **Previous** button (disabled on first page)
- **Next** button (disabled on last page)
- **Status display**: "Page X of Y (Total: Z images)"

Pagination resets when a new folder is selected.

------------------------------------------------------------------------

## ☑️ Checkbox-Based Copy Workflow

Each image has a checkbox below it.

### When a checkbox is checked:

1.  The image file is copied to a user-selected **target folder**
2.  Existing files are not overwritten
3.  The image is marked as processed
4.  The image disappears avoiding reprocessing
5.  New images fill empty slots automatically

This enables fast and efficient photo review workflows.

------------------------------------------------------------------------

## 📂 Target Folder

-   User can choose a destination folder
-   All checked images are copied to this folder
-   If a file already exists:
    -   skip the copy
    -   or optionally generate a unique filename (future extension)

------------------------------------------------------------------------

## 🧠 Data Models

### ImageItem

``` csharp
/// <summary>
/// Represents an image item with its metadata and UI state.
/// </summary>
class ImageItem : ViewModelBase
{
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public DateTime? CreationDate { get; set; }
    public BitmapSource? Thumbnail { get; set; }
    public bool IsChecked { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool HasGpsData => Latitude.HasValue && Longitude.HasValue;
}
```

------------------------------------------------------------------------

### FolderNode

``` csharp
/// <summary>
/// Represents a folder node in the folder tree hierarchy.
/// </summary>
class FolderNode
{
    public string Path { get; set; }
    public string Name { get; set; }
    public ObservableCollection<FolderNode> Children { get; set; }
    public bool IsExpanded { get; set; }
}
```

------------------------------------------------------------------------

## 🧩 Architecture

The application follows the **MVVM pattern** with clean separation of concerns.

    /Models
      ImageItem.cs          - Image data with metadata
      FolderNode.cs         - Folder tree structure

    /ViewModels
      ViewModelBase.cs      - Base class with INotifyPropertyChanged
      RelayCommand.cs       - ICommand implementation
      MainViewModel.cs      - Main application logic

    /Services
      IImageService.cs      - Image operations interface
      ImageService.cs       - Image loading, rotation, deletion, EXIF handling

    /Views
      MainWindow.xaml       - Main application window
      FullscreenImageWindow.xaml - Fullscreen image viewer

    App.xaml.cs             - Application entry point

### Key Design Patterns
- **MVVM**: Clear separation between UI and business logic
- **Dependency Injection**: IImageService injected into ViewModels
- **Async/Await**: All I/O operations are asynchronous
- **Event Handling**: PropertyChanged for UI updates, custom events for scrolling

------------------------------------------------------------------------

## ⚙️ Performance & Technical Requirements

### Image Loading

-   Load thumbnails with configurable size (100-300px)
-   **EXIF orientation automatically applied** using BitmapDecoder
-   Uses:

    ``` csharp
    BitmapDecoder.Create() // Read EXIF metadata
    TransformedBitmap // Apply rotation/scaling
    BitmapCreateOptions.IgnoreImageCache // Force reload after rotation
    ```

-   Load asynchronously with `Task.Run()`
-   Never block the UI thread
-   Images cached in memory with `BitmapCacheOption.OnLoad`

### File Handling

-   Use `System.IO` and `System.Windows.Media.Imaging`
-   Copy files using `File.Copy`
-   Handle IO errors gracefully
-   Application must not crash on file errors
-   **State persistence** using JSON serialization

### Settings Persistence

Settings are saved to `%AppData%\PictureSorter\`:
- `settings.json` - Root folder, target folder, pagination, thumbnail size
- `processed_images.json` - Tracks copied images to prevent reprocessing

------------------------------------------------------------------------

## 🔄 Behavioral Rules

-   Changing folder resets pagination to page 1
-   Only unprocessed images are shown
-   Checkbox triggers immediate copy action
-   UI updates automatically after copy/delete/rotate
-   Application remains responsive with large folders
-   Fullscreen viewer supports arrow key navigation
-   Right-click context menu available on all images
-   Scrollbar automatically scrolls to top on page navigation
-   Settings persist across application sessions
-   EXIF orientation matches Windows File Explorer

------------------------------------------------------------------------

## 🎯 Implemented Features

### ✅ Core Features
- [x] Folder tree navigation
- [x] Configurable pagination (5-100 images)
- [x] Configurable thumbnail sizes
- [x] Checkbox-based image copying
- [x] Auto-refill after copying
- [x] Persistent settings
- [x] Processed images tracking

### ✅ Image Operations
- [x] EXIF orientation support
- [x] Image rotation (90° clockwise and counter-clockwise)
- [x] Image deletion
- [x] Fullscreen viewer
- [x] Arrow key navigation
- [x] Right-click context menus
- [x] GPS coordinate extraction from EXIF
- [x] Google Maps integration for GPS locations

### ✅ UI/UX
- [x] Creation date display
- [x] GPS coordinates display with clickable links
- [x] Custom application icon
- [x] Auto-scroll to top on pagination
- [x] Loading indicators
- [x] Status bar with image count
- [x] Responsive grid layout

------------------------------------------------------------------------

## 🚀 Future Enhancements

-   Multi-select confirmation mode
-   Undo/redo support
-   Batch operations (rotate/delete multiple)
-   EXIF metadata editor
-   Image comparison view (side-by-side)
-   AI-based auto classification
-   Rule-based automatic sorting
-   Drag & drop support
-   Keyboard shortcuts (Delete, R for rotate, etc.)
-   Statistics dashboard (processed images per session)
-   Image filters (date range, file size)
-   Duplicate detection
-   Slideshow mode

------------------------------------------------------------------------

## 📖 Usage Guide

### Getting Started
1. Launch PictureSorter.exe
2. Click **"Browse Root..."** to select your source photo folder
3. Click **"Browse Target..."** to select your destination folder
4. Select a folder from the tree on the left to view its images

### Sorting Workflow
1. Configure your preferred images per page and thumbnail size
2. Browse through images using Previous/Next buttons
3. Check the checkbox below images you want to keep
4. Checked images are automatically copied to target folder
5. Deleted images disappear and new ones fill their place

### Image Operations
- **Fullscreen View**: Click any image
- **Navigate**: Use Left/Right arrow keys in fullscreen
- **Rotate**: Right-click → Rotate 90° Clockwise or Counter-Clockwise
- **Delete**: Right-click → Delete
- **View Location**: Click GPS coordinates or right-click → 📍 View Location (opens Google Maps)

### Settings
All settings are automatically saved:
- Root and target folder paths
- Images per page preference
- Thumbnail size preference
- Processed images list

------------------------------------------------------------------------

## 🛠 Development Notes

-   Target framework: **.NET 8**
-   UI framework: **WPF (Windows Presentation Foundation)**
-   Platform: **Windows 11**
-   Architecture: **MVVM (Model-View-ViewModel)**
-   Language: **C# 12**
-   Design principles: Clarity, stability, performance, and maintainability
-   All source files include XML documentation comments

### Building the Project
```bash
dotnet build
dotnet run
```

### Project Structure
The codebase follows clean architecture principles with:
- **Models**: Data structures (ImageItem, FolderNode)
- **Views**: XAML UI definitions (MainWindow, FullscreenImageWindow)
- **ViewModels**: Business logic and UI state (MainViewModel)
- **Services**: Reusable operations (ImageService)

------------------------------------------------------------------------

## 📌 Instructions for AI or Contributors

> This application is fully implemented according to the specification below.\
> The codebase uses **MVVM architecture**, clean separation of concerns, and reliable file handling.\
> All C# files include **XML documentation comments** for IntelliSense support.\
> Target platform: **Windows 11** with **.NET 8**.\
> The application works fully **offline** with no external dependencies.

### Contribution Guidelines
- Maintain MVVM pattern separation
- Add XML documentation to all public members
- Use async/await for I/O operations
- Handle all exceptions gracefully
- Test with large image collections (1000+ images)
- Ensure UI remains responsive at all times

------------------------------------------------------------------------

## ✅ Project Goal

Create a lightweight but powerful Windows tool for efficiently sorting and managing photos through an intuitive review and organization workflow.

**Status**: ✅ Fully Implemented and Feature Complete

