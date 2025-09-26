using System.Collections.Generic;
using System.Threading.Tasks;
using TodoApp.Models;

namespace TodoApp.Services;

public interface ITodoRepository
{
    Task InitializeAsync();

    Task<IReadOnlyList<TodoItem>> GetAllAsync();

    Task<TodoItem> AddAsync(string title);

    Task UpdateStatusAsync(long id, bool isCompleted);

    Task DeleteAsync(long id);
}
