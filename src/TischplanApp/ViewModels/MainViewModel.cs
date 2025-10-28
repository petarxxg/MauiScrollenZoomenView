using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TischplanApp.Models;

namespace TischplanApp.ViewModels;

/// <summary>
/// ViewModel for the main page displaying positionable items (tables and boxes).
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private ObservableCollection<PositionableItem> _items;

    public MainViewModel()
    {
        _items = new ObservableCollection<PositionableItem>();
        LoadSampleItems();
    }

    /// <summary>
    /// Collection of positionable items to display on the canvas.
    /// </summary>
    public ObservableCollection<PositionableItem> Items
    {
        get => _items;
        set
        {
            if (_items != value)
            {
                _items = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Loads sample items (tables and boxes) for demonstration.
    /// </summary>
    private void LoadSampleItems()
    {
        Items.Clear();

        // Tables
        Items.Add(new TableModel { Id = 1, Name = "Tisch 1", X = 300, Y = 200, Width = 140, Height = 90 });
        Items.Add(new TableModel { Id = 2, Name = "Tisch 2", X = 600, Y = 200, Width = 140, Height = 90 });
        Items.Add(new TableModel { Id = 3, Name = "Tisch 3", X = 900, Y = 200, Width = 140, Height = 90 });

        // Boxes
        Items.Add(new BoxViewModel { Id = 7, Name = "Box A", Content = "Lager", X = 150, Y = 350, Width = 100, Height = 80 });
        Items.Add(new BoxViewModel { Id = 8, Name = "Box B", Content = "Küche", X = 750, Y = 350, Width = 100, Height = 80 });

        // More Tables
        Items.Add(new TableModel { Id = 4, Name = "Tisch 4", X = 300, Y = 500, Width = 140, Height = 90 });
        Items.Add(new TableModel { Id = 5, Name = "Tisch 5", X = 600, Y = 500, Width = 140, Height = 90 });
        Items.Add(new TableModel { Id = 6, Name = "Tisch 6", X = 900, Y = 500, Width = 140, Height = 90 });
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
