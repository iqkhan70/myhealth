# Code Componentization Analysis

## 🔍 Duplicate Code Patterns Identified

### 1. **Page Header Component** ⚠️ HIGH PRIORITY
**Pattern Found:**
```razor
<div class="container-fluid">
    <div class="row">
        <div class="col-12">
            <div class="d-flex justify-content-between align-items-center mb-4">
                <div>
                    <h2><i class="fas fa-{icon} me-2"></i>{Title}</h2>
                    <p class="text-muted mb-0">{Description}</p>
                </div>
                {Optional Action Button}
            </div>
```

**Found in:**
- ✅ ChatHistory.razor
- ✅ ClinicalNotes.razor
- ✅ Journal.razor
- ✅ EmergencyDashboard.razor
- ✅ ClinicalDecisionSupportPage.razor
- ⚠️ Appointments.razor (different structure)
- ⚠️ Patients.razor (different structure)

**Recommendation:** Create `SMPageHeader.razor` component

---

### 2. **Row Expansion Pattern** ⚠️ HIGH PRIORITY
**Pattern Found:**
```csharp
private Dictionary<int, TItem> expandedItems = new();

private async Task OnRowExpand(TItem item)
{
    try
    {
        if (!expandedItems.ContainsKey(item.Id))
        {
            var fullItem = await Service.GetAsync(item.Id);
            if (fullItem != null)
            {
                expandedItems[item.Id] = fullItem;
                StateHasChanged();
            }
        }
    }
    catch (Exception ex)
    {
        NotificationService.Notify(...);
    }
}
```

**Found in:**
- ✅ ChatHistory.razor
- ✅ ClinicalNotes.razor
- ✅ Journal.razor
- ✅ Patients.razor
- ✅ EmergencyDashboard.razor
- ✅ Appointments.razor

**Recommendation:** Create base class or helper for row expansion logic

---

### 3. **InitializeColumns/InitializeActions Pattern** ⚠️ MEDIUM PRIORITY
**Pattern Found:**
```csharp
protected override async Task OnInitializedAsync()
{
    InitializeColumns();
    InitializeActions();
    // ... other initialization
}

private void InitializeColumns() { /* ... */ }
private void InitializeActions() { /* ... */ }
```

**Found in:**
- ✅ All pages using SMDataGrid (6+ pages)

**Recommendation:** This is acceptable - it's a consistent pattern, not necessarily duplicate code

---

### 4. **Notification Helper** ⚠️ HIGH PRIORITY
**Pattern Found:**
```csharp
NotificationService.Notify(new NotificationMessage
{
    Severity = NotificationSeverity.Error,
    Summary = "Error",
    Detail = $"Failed to {action}: {ex.Message}",
    Duration = 6000
});
```

**Found in:** 63+ instances across all pages

**Recommendation:** Create `NotificationHelper` extension methods

---

### 5. **Confirmation Dialog Pattern** ⚠️ MEDIUM PRIORITY
**Pattern Found:**
```csharp
var confirmed = await JSRuntime.InvokeAsync<bool>("confirm",
    $"Are you sure you want to {action}?\n\n" +
    $"Details: {details}\n\n" +
    $"⚠️ This action cannot be undone!");
```

**Found in:**
- ✅ EmergencyDashboard.razor
- ✅ ChatHistory.razor
- ✅ Appointments.razor
- ✅ ClinicalNotes.razor
- ✅ Content.razor

**Recommendation:** Create `ConfirmationDialog` component or helper

---

### 6. **Empty State Component** ⚠️ MEDIUM PRIORITY
**Pattern Found:**
```razor
<div class="empty-state">
    <div class="empty-icon">{icon}</div>
    <h4>{Title}</h4>
    <p class="text-muted">{Message}</p>
</div>
```

**Found in:**
- ✅ Journal.razor
- ✅ Content.razor
- ✅ Multiple other pages

**Recommendation:** Create `SMEmptyState.razor` component

---

### 7. **Loading Spinner** ⚠️ LOW PRIORITY
**Pattern Found:**
```razor
<div class="loading-state">
    <div class="loading-spinner"></div>
    <p>Loading...</p>
</div>
```

**Found in:** Multiple pages

**Recommendation:** Create `SMLoadingSpinner.razor` component

---

## 📊 Summary

| Pattern | Priority | Instances | Component Needed |
|---------|----------|-----------|------------------|
| Page Header | HIGH | 6+ | ✅ `SMPageHeader` |
| Row Expansion | HIGH | 6+ | ✅ Base class/helper |
| Notifications | HIGH | 63+ | ✅ `NotificationHelper` |
| Confirmation Dialog | MEDIUM | 5+ | ✅ `SMConfirmationDialog` |
| Empty State | MEDIUM | 4+ | ✅ `SMEmptyState` |
| Loading Spinner | LOW | 3+ | ✅ `SMLoadingSpinner` |

---

## 🎯 Recommended Implementation Order

1. **NotificationHelper** (Quick win, high impact)
2. **SMPageHeader** (High visibility, used everywhere)
3. **Row Expansion Base Class** (Reduces boilerplate)
4. **SMConfirmationDialog** (Better UX)
5. **SMEmptyState** (Consistent UI)
6. **SMLoadingSpinner** (Nice to have)

---

## ✅ Already Componentized

- ✅ `SMDataGrid` - Excellent componentization
- ✅ `SMModal` - Reusable modal component
- ✅ `BaseService` - Service base class
- ✅ `BaseController` - Controller base class
- ✅ Service interfaces - Good separation

---

## 🔧 Next Steps

1. Create reusable components for high-priority patterns
2. Refactor existing pages to use new components
3. Update documentation
4. Test all pages after refactoring

