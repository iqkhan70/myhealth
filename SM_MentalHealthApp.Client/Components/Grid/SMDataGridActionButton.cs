using Radzen;

namespace SM_MentalHealthApp.Client.Components.Grid;

public class SMDataGridActionButton<TItem>
{
    public string? Icon { get; set; }
    public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Light;
    public Variant Variant { get; set; } = Variant.Flat;
    public ButtonSize Size { get; set; } = ButtonSize.Small;
    public Func<TItem, CancellationToken, Task>? OnClick { get; set; }
    public string? Policy { get; set; }
    public Func<TItem, bool>? Disabled { get; set; }

    public bool HasPolicy => !string.IsNullOrWhiteSpace(Policy);
}

