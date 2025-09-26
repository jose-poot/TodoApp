using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp.ViewModels;

public partial class TodoListViewModel : ObservableObject
{
    private readonly ITodoRepository _repository;
    private readonly System.Threading.SynchronizationContext? _synchronizationContext;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isEmpty = true;

    public ObservableCollection<TodoItem> Items { get; } = new();

    public IAsyncRelayCommand InitializeCommand { get; }

    public IAsyncRelayCommand<string> AddTodoCommand { get; }

    public IAsyncRelayCommand<(TodoItem todo, bool isCompleted)> UpdateTodoStateCommand { get; }

    public IAsyncRelayCommand<TodoItem> DeleteTodoCommand { get; }

    public TodoListViewModel(ITodoRepository repository)
    {
        _repository = repository;
        _synchronizationContext = System.Threading.SynchronizationContext.Current;

        InitializeCommand = new AsyncRelayCommand(InitializeAsync);
        AddTodoCommand = new AsyncRelayCommand<string>(AddTodoAsync, CanAddTodo);
        UpdateTodoStateCommand = new AsyncRelayCommand<(TodoItem todo, bool isCompleted)>(UpdateTodoStateAsync, parameter => parameter.todo is not null);
        DeleteTodoCommand = new AsyncRelayCommand<TodoItem>(DeleteTodoAsync, todo => todo is not null);
    }

    public async Task InitializeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await _repository.InitializeAsync().ConfigureAwait(false);
            await LoadTodosAsync().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadTodosAsync()
    {
        var items = await _repository.GetAllAsync().ConfigureAwait(false);

        await MainThreadInvokeAsync(() =>
        {
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }
            UpdateEmptyState();
        }).ConfigureAwait(false);
    }

    private async Task AddTodoAsync(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var todo = await _repository.AddAsync(title).ConfigureAwait(false);

        await MainThreadInvokeAsync(() =>
        {
            var targetIndex = Items.TakeWhile(x => !x.IsCompleted).Count();
            Items.Insert(targetIndex, todo);
            UpdateEmptyState();
        }).ConfigureAwait(false);
    }

    private bool CanAddTodo(string? title) => !string.IsNullOrWhiteSpace(title);

    private async Task UpdateTodoStateAsync((TodoItem todo, bool isCompleted) parameter)
    {
        var (todo, isCompleted) = parameter;
        if (todo is null)
        {
            return;
        }

        var updated = todo.Clone();
        updated.IsCompleted = isCompleted;
        updated.CompletedAt = updated.IsCompleted ? DateTime.UtcNow : null;

        await _repository.UpdateStatusAsync(updated.Id, updated.IsCompleted).ConfigureAwait(false);

        await MainThreadInvokeAsync(() =>
        {
            var existing = Items.FirstOrDefault(item => item.Id == updated.Id);
            if (existing is null)
            {
                return;
            }

            existing.IsCompleted = updated.IsCompleted;
            existing.CompletedAt = updated.CompletedAt;
            Items.Remove(existing);

            if (updated.IsCompleted)
            {
                Items.Add(existing);
            }
            else
            {
                var activeIndex = Items.TakeWhile(x => !x.IsCompleted).Count();
                Items.Insert(activeIndex, existing);
            }

            UpdateEmptyState();
        }).ConfigureAwait(false);
    }

    private async Task DeleteTodoAsync(TodoItem? todo)
    {
        if (todo is null)
        {
            return;
        }

        await _repository.DeleteAsync(todo.Id).ConfigureAwait(false);

        await MainThreadInvokeAsync(() =>
        {
            var existing = Items.FirstOrDefault(item => item.Id == todo.Id);
            if (existing is not null)
            {
                Items.Remove(existing);
                UpdateEmptyState();
            }
        }).ConfigureAwait(false);
    }

    private void UpdateEmptyState() => IsEmpty = Items.Count == 0;

    private Task MainThreadInvokeAsync(Action action)
    {
        if (_synchronizationContext is null || _synchronizationContext == System.Threading.SynchronizationContext.Current)
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<bool>();
        _synchronizationContext.Post(_ =>
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }, null);

        return tcs.Task;
    }
}
