using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PictureSorter.Services;

/// <summary>
/// Implementation of IImageService providing image operations with EXIF orientation support.
/// </summary>
public class ImageService : IImageService
{
    private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };

    public async Task<BitmapSource?> LoadThumbnailAsync(string filePath)
    {
        return await LoadThumbnailAsync(filePath, 150);
    }

    public async Task<BitmapSource?> LoadThumbnailAsync(string filePath, int thumbnailWidth)
    {
        try
        {
            return await Task.Run(() =>
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                    var frame = decoder.Frames[0];
                    
                    // Get the rotation from metadata
                    var metadata = frame.Metadata as BitmapMetadata;
                    Rotation rotation = Rotation.Rotate0;
                    
                    if (metadata != null)
                    {
                        try
                        {
                            object? orientationQuery = null;
                            if (metadata.ContainsQuery("System.Photo.Orientation"))
                                orientationQuery = metadata.GetQuery("System.Photo.Orientation");
                            else if (metadata.ContainsQuery("/app1/ifd/{ushort=274}"))
                                orientationQuery = metadata.GetQuery("/app1/ifd/{ushort=274}");
                            
                            if (orientationQuery != null)
                            {
                                var orientationValue = Convert.ToUInt16(orientationQuery);
                                rotation = orientationValue switch
                                {
                                    3 => Rotation.Rotate180,
                                    6 => Rotation.Rotate90,
                                    8 => Rotation.Rotate270,
                                    _ => Rotation.Rotate0
                                };
                            }
                        }
                        catch { }
                    }
                    
                    // Create thumbnail with proper scaling
                    double scale = (double)thumbnailWidth / frame.PixelWidth;
                    
                    var transformedBitmap = new TransformedBitmap(frame, new ScaleTransform(scale, scale));
                    
                    // Apply rotation if needed
                    if (rotation != Rotation.Rotate0)
                    {
                        var rotateTransform = rotation switch
                        {
                            Rotation.Rotate90 => new RotateTransform(90),
                            Rotation.Rotate180 => new RotateTransform(180),
                            Rotation.Rotate270 => new RotateTransform(270),
                            _ => new RotateTransform(0)
                        };
                        transformedBitmap = new TransformedBitmap(transformedBitmap, rotateTransform);
                    }
                    
                    // Convert to frozen BitmapImage for UI binding
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(transformedBitmap));
                    
                    using (var memoryStream = new MemoryStream())
                    {
                        encoder.Save(memoryStream);
                        memoryStream.Position = 0;
                        
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = memoryStream;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        
                        return bitmap;
                    }
                }
            });
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> CopyImageAsync(string sourceFilePath, string targetFolderPath)
    {
        try
        {
            if (!Directory.Exists(targetFolderPath))
            {
                Directory.CreateDirectory(targetFolderPath);
            }

            var fileName = Path.GetFileName(sourceFilePath);
            var targetFilePath = Path.Combine(targetFolderPath, fileName);

            // Skip if file already exists
            if (File.Exists(targetFilePath))
            {
                return false;
            }

            await Task.Run(() => File.Copy(sourceFilePath, targetFilePath, false));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public List<string> GetImageFiles(string folderPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
                return new List<string>();

            return Directory.GetFiles(folderPath)
                .Where(IsImageFile)
                .OrderBy(f => f)
                .ToList();
        }
        catch (Exception)
        {
            return new List<string>();
        }
    }

    public bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public async Task<bool> RotateImageAsync(string filePath, int degrees)
    {
        try
        {
            return await Task.Run(() =>
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();

                var rotatedBitmap = new TransformedBitmap(bitmap, new RotateTransform(degrees));

                var encoder = GetEncoder(filePath);
                encoder.Frames.Add(BitmapFrame.Create(rotatedBitmap));

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(stream);
                }

                return true;
            });
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteImageAsync(string filePath)
    {
        try
        {
            await Task.Run(() => File.Delete(filePath));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private BitmapEncoder GetEncoder(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => new JpegBitmapEncoder(),
            ".png" => new PngBitmapEncoder(),
            ".bmp" => new BmpBitmapEncoder(),
            _ => new PngBitmapEncoder()
        };
    }
}
