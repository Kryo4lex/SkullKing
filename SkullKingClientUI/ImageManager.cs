using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkullKingClientUI
{
    /// <summary>
    /// Loads and caches images from a folder into a global dictionary.
    /// Thread-safe singleton. Keys = filename without extension.
    /// </summary>
    public sealed class ImageManager
    {
        private static readonly Lazy<ImageManager> _instance =
            new Lazy<ImageManager>(() => new ImageManager());

        public static ImageManager Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, ImageSource> _images =
            new ConcurrentDictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);

        private ImageManager() { }

        public void LoadFromFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

            foreach (var file in Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp")
                {
                    var key = Path.GetFileNameWithoutExtension(file);
                    _images[key] = LoadImage(file);
                }
            }
        }

        public ImageSource? Get(string name)
        {
            _images.TryGetValue(name, out var img);
            return img;
        }

        public IEnumerable<string> Keys() => _images.Keys;

        private static ImageSource LoadImage(string path)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad; // no file lock
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze(); // cross-thread safe
            return bmp;
        }
    }
}
