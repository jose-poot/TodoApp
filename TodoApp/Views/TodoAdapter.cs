using System;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Card;
using TodoApp.Models;

namespace TodoApp.Views;

public class TodoAdapter : RecyclerView.Adapter
{
    private IList<TodoItem> _items = new List<TodoItem>();

    public event Action<TodoItem, bool>? ToggleCompletedRequested;

    public event Action<TodoItem>? DeleteRequested;

    public override int ItemCount => _items.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var itemView = LayoutInflater.From(parent.Context)!.Inflate(Resource.Layout.item_todo, parent, false)!;
        return new TodoViewHolder(itemView);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is TodoViewHolder todoHolder)
        {
            var item = _items[position];
            todoHolder.Bind(item, HandleToggleRequested, HandleDeleteRequested);
        }
    }

    public void UpdateItems(IEnumerable<TodoItem> items)
    {
        _items = items.ToList();
        NotifyDataSetChanged();
    }

    private void HandleToggleRequested(TodoItem item, bool isCompleted)
    {
        ToggleCompletedRequested?.Invoke(item, isCompleted);
    }

    private void HandleDeleteRequested(TodoItem item)
    {
        DeleteRequested?.Invoke(item);
    }

    private sealed class TodoViewHolder : RecyclerView.ViewHolder
    {
        private readonly CheckBox _completedCheckbox;
        private readonly TextView _titleView;
        private readonly ImageButton _deleteButton;
        private EventHandler<CompoundButton.CheckedChangeEventArgs>? _checkedChangeHandler;
        private EventHandler? _deleteClickHandler;

        public TodoViewHolder(View itemView) : base(itemView)
        {
            _completedCheckbox = itemView.FindViewById<CheckBox>(Resource.Id.todoCompletedCheckbox)!;
            _titleView = itemView.FindViewById<TextView>(Resource.Id.todoTitle)!;
            _deleteButton = itemView.FindViewById<ImageButton>(Resource.Id.deleteTodoButton)!;
        }

        public void Bind(TodoItem item, Action<TodoItem, bool> onCompletedChanged, Action<TodoItem> onDelete)
        {
            (_titleView.Parent as MaterialCardView)?.SetCardBackgroundColor(item.IsCompleted ? Color.Argb(255, 235, 235, 235) : Color.White);

            _titleView.Text = item.Title;
            _titleView.PaintFlags = item.IsCompleted
                ? _titleView.PaintFlags | PaintFlags.StrikeThruText
                : _titleView.PaintFlags & ~PaintFlags.StrikeThruText;

            if (_checkedChangeHandler is not null)
            {
                _completedCheckbox.CheckedChange -= _checkedChangeHandler;
            }

            _completedCheckbox.Checked = item.IsCompleted;
            _checkedChangeHandler = (sender, args) => onCompletedChanged(item, args.IsChecked);
            _completedCheckbox.CheckedChange += _checkedChangeHandler;

            if (_deleteClickHandler is not null)
            {
                _deleteButton.Click -= _deleteClickHandler;
            }

            _deleteClickHandler = (sender, _) => onDelete(item);
            _deleteButton.Click += _deleteClickHandler;
        }
    }
}
