using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFlowSystem
{ 
    /// <summary>
    /// Создаёт и управляет задачами.
    /// Взаимодействует с хранилищем и уведомляет о событиях.
    /// </summary>
    public static class TaskService
    {
        /// <summary>
        /// Создаёт новую задачу с указанными заголовком и описанием.
        /// </summary>
        public static TaskItem CreateTask(
            LocalProfile profile,
            string title,
            string description,
            string status,
            DateTime dueDateUtc)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.Id))
                throw new ArgumentException("Не указан профиль.");
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Введите название задачи.");
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Введите описание задачи.");

            var now = DateTime.UtcNow;
            return new TaskItem
            {
                Id = Guid.NewGuid().ToString("N"),
                ProfileId = profile.Id,
                Title = title.Trim(),
                Description = description.Trim(),
                Status = status,
                DueDateUtc = dueDateUtc,
                CreatedUtc = now,
                UpdatedUtc = now
            };
        }
    }
}
