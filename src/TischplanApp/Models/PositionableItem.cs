using Orderlyze.Foundation.Interfaces;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TischplanApp.Models;

/// <summary>
/// Base class for items that can be positioned on a canvas.
/// </summary>
public abstract partial class PositionableItem : ReactiveObject, INotifyPropertyChanged, IPositionBase
{
    /// <summary>
    /// Unique identifier for the item.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name of the item.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    [Reactive]
    private decimal xposition;
    [Reactive]
    private decimal yposition;
    [Reactive]
    private decimal width;
    [Reactive]
    private decimal height;
    [Reactive]
    private decimal rotation;
    public string Color { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
