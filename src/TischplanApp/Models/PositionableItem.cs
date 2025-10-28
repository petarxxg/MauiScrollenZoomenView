namespace TischplanApp.Models;

/// <summary>
/// Base class for items that can be positioned on a canvas.
/// </summary>
public abstract class PositionableItem
{
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
    public double X { get; set; }

    /// <summary>
    /// Y position in the canvas (in pixels).
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Width of the item view (in pixels).
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Height of the item view (in pixels).
    /// </summary>
    public double Height { get; set; }
}
