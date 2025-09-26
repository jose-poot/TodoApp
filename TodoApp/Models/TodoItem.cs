using System;

namespace TodoApp.Models;

public class TodoItem
{
    public long Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public TodoItem Clone() => (TodoItem)MemberwiseClone();
}
