using TischplanApp.Models;

namespace TischplanApp;

/// <summary>
/// Main page displaying the table floor plan with zoom and pan capabilities.
/// </summary>
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        LoadSampleTables();
    }

    /// <summary>
    /// Loads sample table data for demonstration.
    /// Creates 6 tables distributed across the canvas (reduced for better performance).
    /// </summary>
    private void LoadSampleTables()
    {
        var tables = new List<TableModel>
        {
            new TableModel { Id = 1, Name = "Tisch 1", X = 300, Y = 200, Width = 140, Height = 90 },
            new TableModel { Id = 2, Name = "Tisch 2", X = 600, Y = 200, Width = 140, Height = 90 },
            new TableModel { Id = 3, Name = "Tisch 3", X = 900, Y = 200, Width = 140, Height = 90 },

            new TableModel { Id = 4, Name = "Tisch 4", X = 300, Y = 500, Width = 140, Height = 90 },
            new TableModel { Id = 5, Name = "Tisch 5", X = 600, Y = 500, Width = 140, Height = 90 },
            new TableModel { Id = 6, Name = "Tisch 6", X = 900, Y = 500, Width = 140, Height = 90 }
        };

        ZoomPanCanvas.LoadTables(tables);
    }
}
