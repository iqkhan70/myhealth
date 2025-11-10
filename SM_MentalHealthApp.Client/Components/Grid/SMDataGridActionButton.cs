using Radzen;

namespace SM_MentalHealthApp.Client.Components.Grid;

public class SMDataGridActionButton<TItem>
{
    public string? Icon { get; set; }
    public Func<TItem, string>? IconFunc { get; set; } // Dynamic icon based on row data
    public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Light;
    public Func<TItem, ButtonStyle>? ButtonStyleFunc { get; set; } // Dynamic button style based on row data
    public Variant Variant { get; set; } = Variant.Flat;
    public ButtonSize Size { get; set; } = ButtonSize.Small;
    public Func<TItem, CancellationToken, Task>? OnClick { get; set; }
    public string? Policy { get; set; }
    public Func<TItem, bool>? Disabled { get; set; }
    public string? Tooltip { get; set; }
    public Func<TItem, string>? TooltipFunc { get; set; } // Dynamic tooltip based on row data

    public bool HasPolicy => !string.IsNullOrWhiteSpace(Policy);

    // Helper methods to get the actual values
    public string GetIcon(TItem item) => IconFunc?.Invoke(item) ?? Icon ?? string.Empty;
    public ButtonStyle GetButtonStyle(TItem item) => ButtonStyleFunc?.Invoke(item) ?? ButtonStyle;
    public string GetTooltip(TItem item) => TooltipFunc?.Invoke(item) ?? Tooltip ?? string.Empty;
}

