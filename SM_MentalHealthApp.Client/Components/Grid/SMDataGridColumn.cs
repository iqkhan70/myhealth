using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace SM_MentalHealthApp.Client.Components.Grid;

public class SMDataGridColumn<TItem>
{
    public string? Property { get; set; }
    public string? Title { get; set; }
    public string? Width { get; set; }
    public TextAlign? TextAlign { get; set; }
    public bool? Sortable { get; set; } = true;
    public bool? Filterable { get; set; } = true;
}

public class SMDataGridBadgeColumn<TItem> : SMDataGridColumn<TItem>
{
    public Func<TItem, (string BadgeText, NotificationSeverity BadgeStyle, bool HasBadgeText)> GetBadgeData { get; set; } = null!;
}

public class SMDataGridTextAndBadgeColumn<TItem> : SMDataGridColumn<TItem>
{
    public Func<TItem, (string Text, string? BadgeText, NotificationSeverity BadgeStyle, bool ShouldDisplayBadge, bool HasBadgeText)> GetTextAndBadgeData { get; set; } = null!;
}

public class SMDataGridTemplateColumn<TItem> : SMDataGridColumn<TItem>
{
    public RenderFragment<TItem>? Template { get; set; }
}

