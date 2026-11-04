using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaskFlowSystem
{
    /// <summary>
    /// Локальные фотографии профилей: проверка, уменьшение и хранение копии
    /// в папке приложения. Исходный файл пользователя не изменяется.
    /// </summary>
    public static class ProfileAvatar
    {
        public const long MaxSourceBytes = 10L * 1024 * 1024;

        private const long MaxSourcePixels = 40L * 1000 * 1000;
        private const int MaxStoredDimension = 512;
        private const int PreviewDecodeDimension = 256;

        /// <summary>
        /// Создаёт кисть для предпросмотра выбранного файла.
        /// Исключение содержит понятное пользователю описание проблемы.
        /// </summary>
        public static ImageBrush CreatePreviewBrush(string sourcePath)
        {
            ValidateSource(sourcePath);
            var bitmap = Decode(sourcePath, PreviewDecodeDimension);
            return CreateBrush(bitmap);
        }

        /// <summary>
        /// Сохраняет уменьшенную PNG-копию и возвращает имя файла для профиля.
        /// </summary>
        public static string Save(string profileId, string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                throw new ArgumentException("Не указан идентификатор профиля.", "profileId");
            }

            ValidateSource(sourcePath);

            var decoded = Decode(sourcePath, MaxStoredDimension);
            var converted = new FormatConvertedBitmap(decoded, PixelFormats.Bgra32, null, 0);
            converted.Freeze();
            var image = ScaleDown(converted, MaxStoredDimension);

            var directory = ProfileStore.AvatarsDirectory;
            var fileName = profileId + ".png";
            var targetPath = Path.Combine(directory, fileName);
            var tempPath = targetPath + ".tmp";

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    encoder.Save(stream);
                }

                if (File.Exists(targetPath))
                {
                    var backupPath = targetPath + ".bak";
                    File.Replace(tempPath, targetPath, backupPath, true);
                    TryDeletePath(backupPath);
                }
                else
                {
                    File.Move(tempPath, targetPath);
                }

                return fileName;
            }
            finally
            {
                TryDeletePath(tempPath);
            }
        }

        /// <summary>
        /// Загружает кисть сохранённого аватара. При повреждённом файле
        /// возвращает null — интерфейс покажет цветной аватар с инициалом.
        /// </summary>
        public static ImageBrush TryCreateBrush(string avatarFile, int decodeWidth)
        {
            var path = ResolvePath(avatarFile);
            if (path == null)
            {
                return null;
            }

            try
            {
                var bitmap = Decode(path, Math.Max(64, decodeWidth));
                return CreateBrush(bitmap);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Удаляет локальную копию аватара. Ошибка удаления не должна мешать
        /// удалению самого профиля, поэтому файл просто остаётся осиротевшим.
        /// </summary>
        public static void TryDelete(string avatarFile)
        {
            var path = ResolvePath(avatarFile);
            if (path == null)
            {
                return;
            }

            TryDeletePath(path);
        }

        private static void ValidateSource(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new ArgumentException("Не выбран файл изображения.", "sourcePath");
            }

            var file = new FileInfo(sourcePath);
            if (!file.Exists)
            {
                throw new FileNotFoundException("Файл изображения не найден.", sourcePath);
            }

            if (file.Length > MaxSourceBytes)
            {
                throw new InvalidOperationException(string.Format(
                    "Файл больше {0} МБ. Выберите изображение меньшего размера.",
                    MaxSourceBytes / (1024 * 1024)));
            }

            int width;
            int height;
            ReadPixelSize(sourcePath, out width, out height);

            if (width <= 0 || height <= 0)
            {
                throw new InvalidDataException("Не удалось определить размер изображения.");
            }

            if ((long)width * height > MaxSourcePixels)
            {
                throw new InvalidOperationException("Изображение слишком большое по разрешению.");
            }
        }

        private static void ReadPixelSize(string sourcePath, out int width, out int height)
        {
            using (var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var decoder = BitmapDecoder.Create(
                    stream,
                    BitmapCreateOptions.DelayCreation,
                    BitmapCacheOption.None);

                if (decoder.Frames.Count == 0)
                {
                    throw new InvalidDataException("Файл не содержит изображения.");
                }

                var frame = decoder.Frames[0];
                width = frame.PixelWidth;
                height = frame.PixelHeight;
            }
        }

        private static BitmapSource Decode(string sourcePath, int maxDecodeDimension)
        {
            int width;
            int height;
            ReadPixelSize(sourcePath, out width, out height);

            using (var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;

                if (width > maxDecodeDimension || height > maxDecodeDimension)
                {
                    if (width >= height)
                    {
                        bitmap.DecodePixelWidth = maxDecodeDimension;
                    }
                    else
                    {
                        bitmap.DecodePixelHeight = maxDecodeDimension;
                    }
                }

                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }

        private static BitmapSource ScaleDown(BitmapSource source, int maxDimension)
        {
            var maxSide = Math.Max(source.PixelWidth, source.PixelHeight);
            if (maxSide <= maxDimension)
            {
                return source;
            }

            var scale = (double)maxDimension / maxSide;
            var transformed = new TransformedBitmap(source, new ScaleTransform(scale, scale));
            transformed.Freeze();
            return transformed;
        }

        private static ImageBrush CreateBrush(BitmapSource bitmap)
        {
            var brush = new ImageBrush(bitmap)
            {
                Stretch = Stretch.UniformToFill,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };
            brush.Freeze();
            return brush;
        }

        private static string ResolvePath(string avatarFile)
        {
            if (string.IsNullOrWhiteSpace(avatarFile))
            {
                return null;
            }

            // Имя приходит из локального JSON, поэтому не допускаем путь наружу.
            var fileName = Path.GetFileName(avatarFile);
            if (!string.Equals(fileName, avatarFile, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetExtension(fileName), ".png", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var path = Path.Combine(ProfileStore.AvatarsDirectory, fileName);
            return File.Exists(path) ? path : null;
        }

        private static void TryDeletePath(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Осиротевший файл аватара не критичен для работы приложения.
            }
        }
    }
}
