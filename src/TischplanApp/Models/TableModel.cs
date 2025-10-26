namespace TischplanApp.Models;

/// <summary>
/// Represents a table in the floor plan.
/// </summary>
public class TableModel
{
    /// <summary>
    /// Unique identifier for the table.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name of the table (e.g., "Tisch 1").
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
    /// Width of the table view (in pixels).
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Height of the table view (in pixels).
    /// </summary>
    public double Height { get; set; }
}
