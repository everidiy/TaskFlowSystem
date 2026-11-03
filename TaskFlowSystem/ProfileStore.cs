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
        public string Role { get; set; }
        public string Color { get; set; }
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

        private static string ProfilesPath
        {
            get { return Path.Combine(StorageDirectory, FileName); }
        }

        private static string ActivePath
        {
            get { return Path.Combine(StorageDirectory, ActiveFileName); }
        }

        public static List<LocalProfile> Load()
        {
            try
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

                var serializer = new JavaScriptSerializer();
                var profiles = serializer.Deserialize<List<LocalProfile>>(json) ?? new List<LocalProfile>();
                return profiles.Where(p => p != null && !string.IsNullOrEmpty(p.Id)).ToList();
            }
            catch
            {
                // Повреждённый файл не должен ломать запуск приложения.
                return new List<LocalProfile>();
            }
        }

        public static void Save(IEnumerable<LocalProfile> profiles)
        {
            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(profiles.ToList());
            File.WriteAllText(ProfilesPath, json, Encoding.UTF8);
        }

        public static void Add(LocalProfile profile)
        {
            var profiles = Load();
            profiles.RemoveAll(p => string.Equals(p.Id, profile.Id, StringComparison.OrdinalIgnoreCase));
            profiles.Add(profile);
            Save(profiles);
        }

        public static void Remove(string profileId)
        {
            var profiles = Load();
            profiles.RemoveAll(p => string.Equals(p.Id, profileId, StringComparison.OrdinalIgnoreCase));
            Save(profiles);

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
    }
}
