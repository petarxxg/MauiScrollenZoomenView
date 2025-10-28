using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TischplanApp.Models;

/// <summary>
/// Base class for items that can be positioned on a canvas.
/// </summary>
public abstract class PositionableItem : INotifyPropertyChanged
{
    private double _x;
    private double _y;
    private double _width;
    private double _height;
    private double _scale = 1.0;

    /// <summary>
    /// Unique identifier for the item.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name of the item.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// X position in the canvas (in pixels).
    /// </summary>
    public double X
    {
        get => _x;
        set
        {
            if (_x != value)
            {
                _x = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Y position in the canvas (in pixels).
    /// </summary>
    public double Y
    {
        get => _y;
        set
        {
            if (_y != value)
            {
                _y = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Width of the item view (in pixels).
    /// </summary>
    public double Width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Height of the item view (in pixels).
    /// </summary>
    public double Height
    {
        get => _height;
        set
        {
            if (_height != value)
            {
                _height = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Scale factor for the item (1.0 = 100%).
    /// </summary>
    public double Scale
    {
        get => _scale;
        set
        {
            if (_scale != value)
            {
                _scale = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
