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
    /// Creates 12 tables distributed across the canvas.
    /// </summary>
    private void LoadSampleTables()
    {
        var tables = new List<TableModel>
        {
            new TableModel { Id = 1, Name = "Tisch 1", X = 200, Y = 150, Width = 120, Height = 80 },
            new TableModel { Id = 2, Name = "Tisch 2", X = 450, Y = 150, Width = 120, Height = 80 },
            new TableModel { Id = 3, Name = "Tisch 3", X = 700, Y = 150, Width = 120, Height = 80 },
            new TableModel { Id = 4, Name = "Tisch 4", X = 950, Y = 150, Width = 120, Height = 80 },

            new TableModel { Id = 5, Name = "Tisch 5", X = 200, Y = 400, Width = 120, Height = 80 },
            new TableModel { Id = 6, Name = "Tisch 6", X = 450, Y = 400, Width = 120, Height = 80 },
            new TableModel { Id = 7, Name = "Tisch 7", X = 700, Y = 400, Width = 120, Height = 80 },

            new TableModel { Id = 8, Name = "Tisch 8", X = 1300, Y = 600, Width = 140, Height = 90 },
            new TableModel { Id = 9, Name = "Tisch 9", X = 1600, Y = 600, Width = 140, Height = 90 },

            new TableModel { Id = 10, Name = "Tisch 10", X = 2200, Y = 1200, Width = 150, Height = 100 },
            new TableModel { Id = 11, Name = "Tisch 11", X = 2500, Y = 1500, Width = 120, Height = 80 },
            new TableModel { Id = 12, Name = "Tisch 12", X = 1800, Y = 1600, Width = 130, Height = 85 }
        };

        ZoomPanCanvas.LoadTables(tables);
    }
}
