using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Dialog;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.TextField;
using SQLitePCL;
using TodoApp.Models;
using TodoApp.Services;
using TodoApp.ViewModels;
using TodoApp.Views;

namespace TodoApp;

[Android.App.Activity(Label = "@string/app_name", Theme = "@style/Theme.TodoApp", MainLauncher = true)]
public class MainActivity : AppCompatActivity
{
    private TodoListViewModel? _viewModel;
    private TodoAdapter? _adapter;
    private TextView? _emptyView;
    private RecyclerView? _recyclerView;
    private TodoRepository? _repository;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);

        Batteries_V2.Init();

        _emptyView = FindViewById<TextView>(Resource.Id.emptyView);
        _recyclerView = FindViewById<RecyclerView>(Resource.Id.todoRecyclerView);
        var addButton = FindViewById<FloatingActionButton>(Resource.Id.addTodoButton);

        _repository = new TodoRepository();
        _viewModel = new TodoListViewModel(_repository);

        _adapter = new TodoAdapter();
        _adapter.ToggleCompletedRequested += OnToggleRequested;
        _adapter.DeleteRequested += OnDeleteRequested;

        _recyclerView!.SetAdapter(_adapter);
        _recyclerView.SetHasFixedSize(true);

        _viewModel.Items.CollectionChanged += OnItemsCollectionChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        addButton!.Click += (_, _) => ShowAddTodoDialog();

        _ = LoadAsync();
    }

    protected override void OnDestroy()
    {
        if (_viewModel is { } viewModel)
        {
            viewModel.Items.CollectionChanged -= OnItemsCollectionChanged;
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (_adapter is { } adapter)
        {
            adapter.ToggleCompletedRequested -= OnToggleRequested;
            adapter.DeleteRequested -= OnDeleteRequested;
        }

        _repository?.Dispose();
        _repository = null;

        base.OnDestroy();
    }

    private async Task LoadAsync()
    {
        if (_viewModel is null)
        {
            return;
        }

        try
        {
            await _viewModel.InitializeCommand.ExecuteAsync(null).ConfigureAwait(false);
            await UpdateAdapterAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task UpdateAdapterAsync()
    {
        if (_adapter is null || _viewModel is null)
        {
            return;
        }

        await RunOnUiThreadAsync(() =>
        {
            _adapter.UpdateItems(_viewModel.Items);
            UpdateEmptyState();
        }).ConfigureAwait(false);
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = UpdateAdapterAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TodoListViewModel.IsEmpty))
        {
            RunOnUiThread(UpdateEmptyState);
        }
    }

    private void UpdateEmptyState()
    {
        if (_emptyView is null || _viewModel is null)
        {
            return;
        }

        _emptyView.Visibility = _viewModel.IsEmpty ? ViewStates.Visible : ViewStates.Gone;
    }

    private async void OnToggleRequested(TodoItem item, bool isCompleted)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.UpdateTodoStateCommand.ExecuteAsync((item, isCompleted)).ConfigureAwait(false);
    }

    private async void OnDeleteRequested(TodoItem item)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.DeleteTodoCommand.ExecuteAsync(item).ConfigureAwait(false);
    }

    private void ShowAddTodoDialog()
    {
        if (_viewModel is null)
        {
            return;
        }

        var dialogView = LayoutInflater.Inflate(Resource.Layout.dialog_add_todo, null);
        var input = dialogView!.FindViewById<TextInputEditText>(Resource.Id.todoDescriptionInput);

        var dialog = new MaterialAlertDialogBuilder(this)
            .SetTitle(Resource.String.add_todo_button)
            .SetView(dialogView)
            .SetPositiveButton(Resource.String.add_todo_button, (sender, args) => { })
            .SetNegativeButton(Resource.String.cancel, (sender, args) => { })
            .Create();

        dialog.Show();

        var positiveButton = dialog.GetButton((int)Android.Content.DialogInterface.ButtonPositive);
        positiveButton.Click += async (_, _) =>
        {
            var text = input?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                input!.Error = GetString(Resource.String.add_todo_hint);
                return;
            }

            await _viewModel.AddTodoCommand.ExecuteAsync(text).ConfigureAwait(false);
            dialog.Dismiss();
        };
    }

    private void ShowError(Exception ex)
    {
        if (IsFinishing || IsDestroyed)
        {
            return;
        }

        var message = GetString(Resource.String.todo_list_empty) + "\n" + ex.Message;
        Android.Widget.Toast.MakeText(this, message, ToastLength.Long)?.Show();
    }

    private Task RunOnUiThreadAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        RunOnUiThread(() =>
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
        });
        return tcs.Task;
    }
}
