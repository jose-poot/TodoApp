using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TodoApp.Models;

namespace TodoApp.Services;

public sealed class TodoRepository : ITodoRepository, IDisposable
{
    private const string DatabaseFileName = "todoapp.db3";
    private readonly string _databasePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public TodoRepository()
    {
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Directory.CreateDirectory(basePath);
        _databasePath = Path.Combine(basePath, DatabaseFileName);
    }

    public async Task InitializeAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync().ConfigureAwait(false);

            const string createTableSql = @"
                CREATE TABLE IF NOT EXISTS Todos (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    IsCompleted INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    CompletedAt TEXT NULL
                );";

            await using var command = connection.CreateCommand();
            command.CommandText = createTableSql;
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<TodoItem>> GetAllAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync().ConfigureAwait(false);

            const string query = @"
                SELECT Id, Title, IsCompleted, CreatedAt, CompletedAt
                FROM Todos
                ORDER BY IsCompleted ASC, CreatedAt DESC;";

            await using var command = connection.CreateCommand();
            command.CommandText = query;

            var result = new List<TodoItem>();
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var item = new TodoItem
                {
                    Id = reader.GetInt64(0),
                    Title = reader.GetString(1),
                    IsCompleted = reader.GetInt32(2) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(3)).ToUniversalTime(),
                    CompletedAt = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)).ToUniversalTime()
                };
                result.Add(item);
            }

            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<TodoItem> AddAsync(string title)
    {
        var sanitizedTitle = title.Trim();
        if (string.IsNullOrEmpty(sanitizedTitle))
        {
            throw new ArgumentException("The title cannot be empty", nameof(title));
        }

        var now = DateTime.UtcNow;

        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync().ConfigureAwait(false);

            const string insertSql = @"
                INSERT INTO Todos (Title, IsCompleted, CreatedAt, CompletedAt)
                VALUES ($title, $isCompleted, $createdAt, $completedAt);";

            await using var command = connection.CreateCommand();
            command.CommandText = insertSql;
            command.Parameters.AddWithValue("$title", sanitizedTitle);
            command.Parameters.AddWithValue("$isCompleted", 0);
            command.Parameters.AddWithValue("$createdAt", now.ToString("o"));
            command.Parameters.AddWithValue("$completedAt", DBNull.Value);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);

            var id = connection.LastInsertRowId;

            return new TodoItem
            {
                Id = id,
                Title = sanitizedTitle,
                IsCompleted = false,
                CreatedAt = now
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateStatusAsync(long id, bool isCompleted)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync().ConfigureAwait(false);

            const string updateSql = @"
                UPDATE Todos
                SET IsCompleted = $isCompleted,
                    CompletedAt = $completedAt
                WHERE Id = $id;";

            var completedAt = isCompleted ? DateTime.UtcNow.ToString("o") : null;

            await using var command = connection.CreateCommand();
            command.CommandText = updateSql;
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$isCompleted", isCompleted ? 1 : 0);
            command.Parameters.AddWithValue("$completedAt", (object?)completedAt ?? DBNull.Value);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteAsync(long id)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync().ConfigureAwait(false);

            const string deleteSql = "DELETE FROM Todos WHERE Id = $id;";

            await using var command = connection.CreateCommand();
            command.CommandText = deleteSql;
            command.Parameters.AddWithValue("$id", id);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _semaphore.Dispose();
        _disposed = true;
    }

    private SqliteConnection CreateConnection() => new($"Data Source={_databasePath}");
}
