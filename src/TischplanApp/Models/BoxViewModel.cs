namespace TischplanApp.Models;

/// <summary>
/// Represents a box in the floor plan.
/// </summary>
public class BoxViewModel : PositionableItem
{
    /// <summary>
    /// Content or description of the box.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
