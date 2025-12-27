# Picture Sorter

A Windows desktop application for efficiently sorting and managing photos with an intuitive interface and powerful features.

The application is designed for **manual photo review workflows**, where users can quickly browse, preview, and organize their image collections.

**Creator:** Robert Erlandsson

**Project Statistics:**
- **Total Lines of Code:** 5,700+
- **C# Code:** 5,376 lines
- **XAML UI:** 363 lines
- **Source Files:** 181 (including generated files)

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
-   **Multi-Select Mode**
    -   Check multiple images across different folders
    -   Persistent selection tracking
    -   "Copy X Selected" button for batch operations
    -   Selections remain when navigating between folders
-   **Fullscreen Image Viewer**
    -   Click any image to view it fullscreen
    -   Navigate with arrow keys (Left/Right)
    -   Press **Space** to move image to target folder
    -   Right-click menu for rotate and delete
-   **Quick Move Button**
    -   Small ↗ button next to each checkbox
    -   Instantly moves image to target folder
    -   One-click operation for fast sorting
-   **Image Manipulation**
    -   Rotate images 90° clockwise or counter-clockwise
    -   Delete unwanted images
    -   Operations work in both grid and fullscreen views
-   **GPS Location Support**
    -   Automatically reads GPS coordinates from EXIF metadata
    -   Displays coordinates under images with location data
    -   Click coordinates or right-click → "📍 View Location" to open in Google Maps
    -   Supports UInt64 packed rational format for GPS data
-   **Sorting Options**
    -   Sort by Creation Date (default, newest first)
    -   Sort by File Name (alphabetical)
    -   Sort by File Size
    -   Toggle sort direction with ↑↓ button
-   **Configurable Thumbnail Sizes**
    -   5 size options: Small (100px) to XX-Large (300px)
-   **Persistent Settings**
    -   Remembers root and target folders
    -   Saves pagination and thumbnail size preferences
    -   Tracks processed images across sessions
    -   Maintains sort preferences
    -   **Profile System** for multiple workspace configurations
-   **Image Metadata Display**
    -   Shows creation date for each photo
    -   Displays GPS coordinates when available
    -   File name display
-   **Custom Application Icon**
-   **Optimized Performance**
    -   Cancellation tokens prevent race conditions
    -   Fast folder switching without errors
    -   Responsive UI even with large image collections
-   **Undo/Redo Support**
    -   Undo move, delete, rotate, and copy operations
    -   Up to 50 levels of undo history
    -   Automatic state restoration for all operations
    -   Visual indicators showing what can be undone/redone
    -   Full integration with processed images tracking
    -   Keyboard shortcuts: Ctrl+Z (Undo), Ctrl+Y (Redo)

------------------------------------------------------------------------

## 🧭 Application Overview

    ----------------------------------------------------------
    | Profile: [▼] [New] [Delete] (Auto-saves) [↶Undo][↷Redo]|
    | Root: [Browse]  Target: [Browse]                      |
    | Images/page: [▼] Size: [▼] Sort: [▼] [↑↓]            |
    |--------------------------------------------------------|
    | Folder Tree      | Image Grid (configurable)          |
    |                  |--------------------------------------|
    |  📁 2024         | [img]  [img]  [img]  [img]  [img]   |
    |    📁 Vacation   |  ☑↗    ☐↗    ☐↗    ☐↗    ☐↗     |
    |    📁 Birthday   | 📍GPS  Date   Date   Date   Date    |
    |                  |                                      |
    |                  | [img]  [img]  [img]  [img]  [img]   |
    |                  |  ☐↗    ☐↗    ☐↗    ☐↗    ☐↗     |
    |                  | Date   Date   Date   Date   Date    |
    |                  |--------------------------------------|
    |                  | ← Previous [Copy 3 Selected] Next → |
    ----------------------------------------------------------

### Features in Action
- Click any image → Opens fullscreen viewer
- Press **Space** in fullscreen → Moves image to target folder
- Right-click image → Rotate, Delete, or View Location menu
- Check checkbox → Selects image for batch copy
- Click ↗ button → Instantly moves image to target
- Click "Copy X Selected" → Batch copies all checked images
- **↶ Undo button** → Reverses last operation (move, delete, rotate, copy)
- **↷ Redo button** → Re-applies last undone operation
- Scrollbar resets to top on page navigation
- Sort dropdown → Change sorting order
- ↑↓ button → Toggle sort direction

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

## ☑️ Multi-Select and Copy Workflow

### Multi-Select Confirmation Mode
1. Check multiple images across any folders
2. Selections persist when navigating between folders
3. "Copy X Selected" button shows total selected count
4. Click button to batch copy all selected images
5. Selected images are copied and removed from view

### Quick Move Button
- Each image has a ↗ button next to the checkbox
- Click to instantly move the image to target folder
- No confirmation needed - immediate action
- Faster than checkbox for single-image sorting

### Checkbox Behavior
- Check = Add to selection
- Uncheck = Remove from selection
- Selection count updates in real-time
- Selections maintained across folder navigation

This enables both fast single-image sorting and efficient batch operations.

------------------------------------------------------------------------

## 📂 Target Folder

-   User can choose a destination folder
-   All checked images are copied to this folder
-   If a file already exists:
    -   skip the copy
    -   or optionally generate a unique filename (future extension)

------------------------------------------------------------------------

## 🧠 Data Models

### Profile

``` csharp
/// <summary>
/// Represents a saved workspace configuration with all settings and state.
/// </summary>
class Profile
{
    public string Name { get; set; }
    public string? RootFolderPath { get; set; }
    public string? TargetFolderPath { get; set; }
    public HashSet<string> ProcessedImages { get; set; }
    public HashSet<string> SelectedImages { get; set; }
    public int ImagesPerPage { get; set; }
    public int ThumbnailSize { get; set; }
    public SortOption SortBy { get; set; }
    public bool SortDescending { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModified { get; set; }
}
```

------------------------------------------------------------------------

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
      Profile.cs            - Profile/workspace configuration
      ImageItem.cs          - Image data with metadata
      FolderNode.cs         - Folder tree structure
      
    /Commands
      IUndoableCommand.cs   - Interface for undoable operations
      UndoRedoManager.cs    - Undo/redo stack management
      MoveImageCommand.cs   - Undoable move operation
      DeleteImageCommand.cs - Undoable delete with file backup
      RotateImageCommand.cs - Undoable rotation
      CopyImagesCommand.cs  - Undoable batch copy

    /ViewModels
      ViewModelBase.cs      - Base class with INotifyPropertyChanged
      RelayCommand.cs       - ICommand implementation
      MainViewModel.cs      - Main application logic

    /Services
      IImageService.cs      - Image operations interface
      ImageService.cs       - Image loading, rotation, deletion, EXIF handling

    Command Pattern**: Undoable operations with execute/undo methods
- **Dependency Injection**: IImageService injected into ViewModels
- **Async/Await**: All I/O operations are asynchronous
- **Cancellation Tokens**: Prevent race conditions during folder switching
- **Event Handling**: PropertyChanged for UI updates, custom events for scrolling
- **Persistent State**: JSON serialization for settings and selections
- **Undo/Redo Stack**: Command pattern for reversible opera

### Key Design Patterns
- **MVVM**: Clear separation between UI and business logic
- **Dependency Injection**: IImageService injected into ViewModels
- **Async/Await**: All I/O operations are asynchronous
- **Cancellation Tokens**: Prevent race conditions during folder switching
- **Event Handling**: PropertyChanged for UI updates, custom events for scrolling
- **Persistent State**: JSON serialization for settings and selections

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
- `Profiles\*.json` - Individual profile configurations with complete workspace state

### Profile System

Profiles store complete workspace configurations:
- **Auto-save**: All changes automatically saved to active profile
- **Profile switching**: Select from dropdown to load different workspace
- **Multiple workspaces**: Create profiles for different photo projects
- **State preservation**: Tracks folders, processed images, selections, and all settings
- **Isolated data**: Each profile maintains independent state

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
- [x] Configurable thumbnail sizes (100-300px)
- [x] Multi-select confirmation mode
- [x] Persistent selection across folders
- [x] Quick move button (↗) for instant transfers
- [x] Batch copy operations
- [x] Auto-refill after copying/moving
- [x] Persistent settings
- [x] Processed images tracking
- [x] Sorting options (Date, Name, Size)
- [x] Toggle sort direction
- [x] Profile system for multiple workspaces
- [x] Auto-save profiles

### ✅ Image Operations
- [x] EXIF orientation support
- [x] Image rotation (90° clockwise and counter-clockwise)
- [x] Image deletion
- [x] Move operation (instant transfer)
- [x] Copy operation (preserves original)
- [x] Fullscreen viewer
- [x] Arrow key navigation
- [x] Space bar to move in fullscreen
- [x] Right-click context menus
- [x] GPS coordinate extraction from EXIF
- [x] Google Maps integration for GPS locations

### ✅ UI/UX
- [x] Creation date display
- [x] GPS coordinates display with clickable links
- [x] Custom application icon
- [x] Auto-scroll to top on pagination
- [x] Loading indicators
- [x] Status bar with operation feedback
- [x] Responsive grid layout
- [x] Slim dropdown styling
- [x] Undo/redo support (up to 50 levels)
- [x] Undoable move, delete, rotate, and copy operations
- [x] Cancellation tokens for race condition prevention
- [x] Fast folder switching without errors

------------------------------------------------------------------------

## AI-based auto classification
-   Rule-based automatic sorting
-   Drag & drop support
-   More keyboard shortcuts (Delete, R for rotate, etc.)
-   Statistics dashboard (processed images per session)
-   Image filters (date range, file size)
-   Duplicate detection
-   Slideshow mode
-   Video file support
-   Cloud storage integration
-   EXIF metadata editor
-   Image comparison view (side-by-side)
-   Video file support
-   Cloud storage integration

------------------------------------------------------------------------

## 📖 Usage Guide

### Getting Started
1. Launch PictureSorter.exe
2. Create a profile by clicking **"New"** or use the default profile
3. Click **"Browse Root..."** to select your source photo folder
4. Click **"Browse Target..."** to select your destination folder
5. Select a folder from the tree on the left to view its images

### Profile Management
- **Create Profile**: Click "New" button, enter a name for your workspace
- **Switch Profile**: Select from the dropdown - automatically loads that workspace
- **Delete Profile**: Select profile and click "Delete" button
- **Auto-save**: All changes (folders, selections, settings) automatically saved

Each profile maintains:
- Root and target folder paths
- Processed images history
- Selected images for batch operations
- Images per page setting
- Thumbnail size preference
- Sort preferences

### Sorting Workflow
1. Configure your preferred images per page and thumbnail size
2. Choose sort order (Creation Date, File Name, or File Size)
3. Toggle sort direction with ↑↓ button if needed
4. Browse through images using Previous/Next buttons
5. Use quick move button (↗) for instant single-image transfers
6. Or check multiple images and use "Copy X Selected" for batch operations
7. Moved/copied images disappear and new ones fill their place

### Image Operations
- **Fullscreen View**: Click any image
- **Navigate**: Use Left/Right arrow keys in fullscreen
- **Quick Move**: Press Space in fullscreen to move image to target
- **Instant Move**: Click ↗ button next to checkbox
- **Batch Copy**: Check multiple images, click "Copy X Selected"
- **Rotate**: Right-click → Rotate 90° Clockwise or Counter-Clockwise
- **Undo**: Click ↶ Undo button or press Ctrl+Z to reverse last operation
- **Redo**: Click ↷ Redo button or press Ctrl+Y to re-apply undone operation
- **Delete**: Right-click → Delete
- **View Location**: Click GPS coordinates or right-click → 📍 View Location (opens Google Maps)

### Settings
All settings are automatically saved to the active profile:
- Root and target folder paths
- Images per page preference
- Thumbnail size preference
- Sort by and sort direction preferences
- Processed images list
- Selected images for batch operations

Switch between profiles to manage different photo projects independently.

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

