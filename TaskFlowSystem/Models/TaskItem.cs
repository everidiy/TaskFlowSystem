using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFlowSystem
{
    /// <summary>
    /// Локальная задача профиля. Содержит только данные, 
    /// которые нужны для отображения в списке задач.
    /// </summary>
    public class TaskItem
    {
        public string Id { get; set; }
        public string ProfileId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime DueDateUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public bool IsArchived { get; set; }
    }
}
