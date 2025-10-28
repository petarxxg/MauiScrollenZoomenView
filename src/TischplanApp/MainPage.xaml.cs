using TischplanApp.ViewModels;

namespace TischplanApp;

/// <summary>
/// Main page displaying the table floor plan with zoom and pan capabilities.
/// </summary>
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new MainViewModel();
    }
}
