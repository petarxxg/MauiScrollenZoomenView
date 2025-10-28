using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TischplanApp.Models;

namespace TischplanApp.ViewModels;

/// <summary>
/// ViewModel for the main page displaying positionable items (tables and boxes).
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private ObservableCollection<PositionableItem> _items;
    private bool _isEditMode;
    private bool _showGrid;

    public MainViewModel()
    {
        _items = new ObservableCollection<PositionableItem>();
        LoadSampleItems();
        ItemTappedCommand = new Command<object>(OnItemTapped);
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
    /// Indicates whether the canvas is in edit mode (allows dragging and resizing items).
    /// </summary>
    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (_isEditMode != value)
            {
                _isEditMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditModeText));
            }
        }
    }

    /// <summary>
    /// Text to display on the edit mode toggle button.
    /// </summary>
    public string EditModeText => IsEditMode ? "Ansicht-Modus" : "Bearbeitungs-Modus";

    /// <summary>
    /// Indicates whether the grid is visible on the canvas.
    /// </summary>
    public bool ShowGrid
    {
        get => _showGrid;
        set
        {
            if (_showGrid != value)
            {
                _showGrid = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GridModeText));
            }
        }
    }

    /// <summary>
    /// Text to display on the grid toggle button.
    /// </summary>
    public string GridModeText => ShowGrid ? "Grid ausblenden" : "Grid einblenden";

    /// <summary>
    /// Command that is executed when an item is tapped.
    /// </summary>
    public ICommand ItemTappedCommand { get; }

    /// <summary>
    /// Toggles the edit mode on/off.
    /// </summary>
    public void ToggleEditMode()
    {
        IsEditMode = !IsEditMode;
    }

    /// <summary>
    /// Toggles the grid visibility on/off.
    /// </summary>
    public void ToggleGridMode()
    {
        ShowGrid = !ShowGrid;
    }

    /// <summary>
    /// Handles item tapped event and displays item position.
    /// </summary>
    private async void OnItemTapped(object item)
    {
        if (item is PositionableItem posItem)
        {
            await Application.Current?.MainPage?.DisplayAlert(
                "Item Info",
                $"Name: {posItem.Name}\nX: {posItem.Xposition}\nY: {posItem.Yposition}\nWidth: {posItem.Width}\nHeight: {posItem.Height}",
                "OK");
        }
    }

    /// <summary>
    /// Loads sample items (tables and boxes) for demonstration.
    /// </summary>
    private void LoadSampleItems()
    {
        Items.Clear();

        // Tables
        Items.Add(new TableModel { Id = 1, Name = "Tisch 1", Xposition = 300, Yposition = 200, Width = 140, Height = 90 });
        Items.Add(new TableModel { Id = 2, Name = "Tisch 2", Xposition = 600, Yposition = 200, Width = 140, Height = 90 });
        Items.Add(new TableModel { Id = 3, Name = "Tisch 3", Xposition = 900, Yposition = 200, Width = 140, Height = 90 });

        // Boxes
        Items.Add(new BoxViewModel { Id = 7, Name = "Box A", Content = "Lager", Xposition = 150, Yposition = 350, Width = 100, Height = 80 });
        Items.Add(new BoxViewModel { Id = 8, Name = "Box B", Content = "Küche", Xposition = 750, Yposition = 350, Width = 100, Height = 80 });

        // More Tables
        Items.Add(new TableModel { Id = 4, Name = "Tisch 4", Xposition = 300, Yposition = 500, Width = 140, Height = 90 });
        Items.Add(new TableModel { Id = 5, Name = "Tisch 5", Xposition = 600, Yposition = 500, Width = 140, Height = 90 });
        Items.Add(new TableModel { Id = 6, Name = "Tisch 6", Xposition = 900, Yposition = 500, Width = 140, Height = 90 });
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
