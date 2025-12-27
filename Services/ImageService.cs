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
                // Add timestamp to force cache invalidation
                var fileInfo = new FileInfo(filePath);
                var cacheKey = $"{filePath}?t={fileInfo.LastWriteTime.Ticks}";
                
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.None);
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
                // Load the original image
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();

                // Apply rotation transformation
                var rotatedBitmap = new TransformedBitmap(bitmap, new RotateTransform(degrees));

                // Save back to file
                var encoder = GetEncoder(filePath);
                encoder.Frames.Add(BitmapFrame.Create(rotatedBitmap));

                // Create a temporary file first
                var tempFile = Path.GetTempFileName();
                using (var stream = new FileStream(tempFile, FileMode.Create))
                {
                    encoder.Save(stream);
                }

                // Replace original file with rotated version
                File.Delete(filePath);
                File.Move(tempFile, filePath);

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

    public async Task<(double? Latitude, double? Longitude)?> GetGpsCoordinatesAsync(string filePath)
    {
        try
        {
            return await Task.Run<(double? Latitude, double? Longitude)?>(() =>
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
                    var frame = decoder.Frames[0];
                    var metadata = frame.Metadata as BitmapMetadata;

                    if (metadata != null)
                    {
                        try
                        {
                            // Try all possible GPS tag variations
                            object? latData = null;
                            object? latRef = null;
                            object? lonData = null;
                            object? lonRef = null;

                            // Try ushort format (most common)
                            latData = metadata.GetQuery("/app1/ifd/gps/{ushort=2}");
                            latRef = metadata.GetQuery("/app1/ifd/gps/{ushort=1}");
                            lonData = metadata.GetQuery("/app1/ifd/gps/{ushort=4}");
                            lonRef = metadata.GetQuery("/app1/ifd/gps/{ushort=3}");

                            // Try ulong format if ushort didn't work
                            if (latData == null)
                            {
                                latData = metadata.GetQuery("/app1/ifd/gps/{ulong=2}");
                                latRef = metadata.GetQuery("/app1/ifd/gps/{ulong=1}");
                                lonData = metadata.GetQuery("/app1/ifd/gps/{ulong=4}");
                                lonRef = metadata.GetQuery("/app1/ifd/gps/{ulong=3}");
                            }

                            // Try without braces
                            if (latData == null)
                            {
                                latData = metadata.GetQuery("/app1/ifd/gps/subifd:2");
                                latRef = metadata.GetQuery("/app1/ifd/gps/subifd:1");
                                lonData = metadata.GetQuery("/app1/ifd/gps/subifd:4");
                                lonRef = metadata.GetQuery("/app1/ifd/gps/subifd:3");
                            }

                            if (latData != null && lonData != null)
                            {
                                double? latitude = ParseGpsCoordinate(latData);
                                double? longitude = ParseGpsCoordinate(lonData);

                                if (latitude.HasValue && longitude.HasValue)
                                {
                                    string latRefStr = latRef?.ToString() ?? "";
                                    string lonRefStr = lonRef?.ToString() ?? "";
                                    
                                    if (latRefStr == "S") latitude = -latitude;
                                    if (lonRefStr == "W") longitude = -longitude;
                                    
                                    return (latitude, longitude);
                                }
                            }
                        }
                        catch { }
                    }
                    return null;
                }
            });
        }
        catch { return null; }
    }

    private double? ParseGpsCoordinate(object coordinateData)
    {
        try
        {
            // Handle UInt64[] with length 3
            if (coordinateData is ulong[] ulongArray && ulongArray.Length == 3)
            {
                // Each UInt64 encodes a rational: lower 32 bits = numerator, upper 32 bits = denominator
                double degrees = GetRationalValue(ulongArray[0]);
                double minutes = GetRationalValue(ulongArray[1]);
                double seconds = GetRationalValue(ulongArray[2]);
                
                var result = degrees + (minutes / 60.0) + (seconds / 3600.0);
                return result;
            }

            // Handle ulong[] format with 6 values - traditional format
            if (coordinateData is ulong[] ulongArray6 && ulongArray6.Length >= 6)
            {
                double degrees = (double)ulongArray6[0] / ulongArray6[1];
                double minutes = (double)ulongArray6[2] / ulongArray6[3];
                double seconds = (double)ulongArray6[4] / ulongArray6[5];
                var result = degrees + (minutes / 60.0) + (seconds / 3600.0);
                return result;
            }
            
            // Handle long[] format
            if (coordinateData is long[] longArray && longArray.Length >= 6)
            {
                double degrees = (double)longArray[0] / longArray[1];
                double minutes = (double)longArray[2] / longArray[3];
                double seconds = (double)longArray[4] / longArray[5];
                var result = degrees + (minutes / 60.0) + (seconds / 3600.0);
                return result;
            }

            // Handle uint[] format
            if (coordinateData is uint[] uintArray && uintArray.Length >= 6)
            {
                double degrees = (double)uintArray[0] / uintArray[1];
                double minutes = (double)uintArray[2] / uintArray[3];
                double seconds = (double)uintArray[4] / uintArray[5];
                var result = degrees + (minutes / 60.0) + (seconds / 3600.0);
                return result;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"GPS Parse Error: {ex.Message}", "GPS Error");
        }
        
        return null;
    }

    private double GetRationalValue(ulong packed)
    {
        // Extract numerator (lower 32 bits) and denominator (upper 32 bits)
        uint numerator = (uint)(packed & 0xFFFFFFFF);
        uint denominator = (uint)(packed >> 32);
        
        if (denominator == 0)
            return 0;
            
        return (double)numerator / denominator;
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
