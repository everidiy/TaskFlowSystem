using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace TaskFlowSystem
{
    /// <summary>
    /// Локальный профиль пользователя. Пароли не используются —
    /// достаточно выбрать существующий профиль или создать новый.
    /// </summary>
    public class LocalProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public string AvatarFile { get; set; }
        public DateTime CreatedUtc { get; set; }

        [ScriptIgnore]
        public string Initial
        {
            get
            {
                var name = (Name ?? string.Empty).Trim();
                if (name.Length == 0)
                {
                    return "?";
                }

                var first = name[0];
                return char.IsLetter(first) ? first.ToString().ToUpperInvariant() : "#";
            }
        }
    }

    /// <summary>
    /// Хранилище локальных профилей в JSON-файле рядом с настройками пользователя.
    /// </summary>
    public static class ProfileStore
    {
        private const string FileName = "profiles.json";
        private const string ActiveFileName = "active-profile.txt";

        public static string StorageDirectory
        {
            get
            {
                var root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TaskFlowSystem");
                Directory.CreateDirectory(root);
                return root;
            }
        }

        /// <summary>
        /// Папка для локальных копий фотографий профилей.
        /// </summary>
        public static string AvatarsDirectory
        {
            get
            {
                var path = Path.Combine(StorageDirectory, "avatars");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        private static string ProfilesPath
        {
            get { return Path.Combine(StorageDirectory, FileName); }
        }

        private static string ActivePath
        {
            get { return Path.Combine(StorageDirectory, ActiveFileName); }
        }

        /// <summary>
        /// Загружает профили. Ошибку чтения не скрываем: вызывающий код должен
        /// показать её пользователю и не перезаписывать повреждённый файл.
        /// </summary>
        public static List<LocalProfile> Load()
        {
            if (!File.Exists(ProfilesPath))
            {
                return new List<LocalProfile>();
            }

            var json = File.ReadAllText(ProfilesPath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<LocalProfile>();
            }

            List<StoredProfile> storedProfiles;
            try
            {
                var serializer = new JavaScriptSerializer();
                storedProfiles = serializer.Deserialize<List<StoredProfile>>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Файл профилей повреждён и не может быть прочитан.", ex);
            }

            var profiles = new List<LocalProfile>();
            if (storedProfiles == null)
            {
                return profiles;
            }

            foreach (var stored in storedProfiles)
            {
                var profile = ToProfile(stored);
                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }

            return profiles;
        }

        /// <summary>
        /// Сохраняет профили через временный файл, чтобы сбой записи не оставил
        /// частично записанный JSON.
        /// </summary>
        public static void Save(IEnumerable<LocalProfile> profiles)
        {
            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(profiles.ToList());

            var targetPath = ProfilesPath;
            var tempPath = targetPath + ".tmp";

            try
            {
                File.WriteAllText(tempPath, json, new UTF8Encoding(false));

                if (File.Exists(targetPath))
                {
                    var backupPath = targetPath + ".bak";
                    File.Replace(tempPath, targetPath, backupPath, true);
                    TryDeleteFile(backupPath);
                }
                else
                {
                    File.Move(tempPath, targetPath);
                }
            }
            finally
            {
                TryDeleteFile(tempPath);
            }
        }

        public static void Add(LocalProfile profile)
        {
            var profiles = Load();
            var existing = profiles.FirstOrDefault(
                p => string.Equals(p.Id, profile.Id, StringComparison.OrdinalIgnoreCase));

            profiles.RemoveAll(p => string.Equals(p.Id, profile.Id, StringComparison.OrdinalIgnoreCase));
            profiles.Add(profile);
            Save(profiles);

            if (existing != null &&
                !string.IsNullOrWhiteSpace(existing.AvatarFile) &&
                !string.Equals(existing.AvatarFile, profile.AvatarFile, StringComparison.OrdinalIgnoreCase))
            {
                // Новый JSON уже сохранён — старую фотографию можно удалить.
                ProfileAvatar.TryDelete(existing.AvatarFile);
            }
        }

        public static void Remove(string profileId)
        {
            var profiles = Load();
            var profile = profiles.FirstOrDefault(
                p => string.Equals(p.Id, profileId, StringComparison.OrdinalIgnoreCase));

            if (profile == null)
            {
                return;
            }

            profiles.Remove(profile);
            Save(profiles);

            // Осиротевший файл аватара не должен мешать удалению профиля.
            ProfileAvatar.TryDelete(profile.AvatarFile);

            if (string.Equals(GetActiveId(), profileId, StringComparison.OrdinalIgnoreCase))
            {
                ClearActive();
            }
        }

        public static string GetActiveId()
        {
            try
            {
                return File.Exists(ActivePath) ? File.ReadAllText(ActivePath, Encoding.UTF8).Trim() : null;
            }
            catch
            {
                return null;
            }
        }

        public static void SetActiveId(string profileId)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                ClearActive();
                return;
            }

            File.WriteAllText(ActivePath, profileId, Encoding.UTF8);
        }

        public static void ClearActive()
        {
            try
            {
                if (File.Exists(ActivePath))
                {
                    File.Delete(ActivePath);
                }
            }
            catch
            {
                // Игнорируем ошибки очистки — они не критичны.
            }
        }

        private static LocalProfile ToProfile(StoredProfile stored)
        {
            if (stored == null || string.IsNullOrWhiteSpace(stored.Id))
            {
                return null;
            }

            var description = stored.Description;
            if (string.IsNullOrWhiteSpace(description) && !string.IsNullOrWhiteSpace(stored.Role))
            {
                // Совместимость с профилями, сохранёнными до замены роли описанием.
                var legacyRole = stored.Role.Trim();
                if (!string.Equals(legacyRole, "Гость", StringComparison.OrdinalIgnoreCase))
                {
                    description = legacyRole;
                }
            }

            return new LocalProfile
            {
                Id = stored.Id,
                Name = stored.Name,
                Description = description,
                Color = stored.Color,
                AvatarFile = stored.AvatarFile,
                CreatedUtc = stored.CreatedUtc
            };
        }

        private static void TryDeleteFile(string path)
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
                // Временный или резервный файл не критичен для работы профиля.
            }
        }

        /// <summary>
        /// Формат файла на диске. Поле Role оставлено только для чтения старых
        /// профилей, в новом формате используется Description.
        /// </summary>
        public sealed class StoredProfile
        {
            public StoredProfile()
            {
            }

            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Role { get; set; }
            public string Color { get; set; }
            public string AvatarFile { get; set; }
            public DateTime CreatedUtc { get; set; }
        }
    }
}
