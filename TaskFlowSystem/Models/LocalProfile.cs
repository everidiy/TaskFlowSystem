using System;
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
}
