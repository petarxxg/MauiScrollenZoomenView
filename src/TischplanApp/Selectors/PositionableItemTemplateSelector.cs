using TischplanApp.Models;

namespace TischplanApp.Selectors;

/// <summary>
/// DataTemplateSelector that chooses different templates based on the type of PositionableItem.
/// </summary>
public class PositionableItemTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Template for TableModel items.
    /// </summary>
    public DataTemplate? TableTemplate { get; set; }

    /// <summary>
    /// Template for BoxViewModel items.
    /// </summary>
    public DataTemplate? BoxTemplate { get; set; }

    protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
    {
        return item switch
        {
            TableModel => TableTemplate,
            BoxViewModel => BoxTemplate,
            _ => null
        };
    }
}
