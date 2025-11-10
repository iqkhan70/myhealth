# Pagination Implementation Guide

## ✅ Current Status

The `SMDataGrid` component now supports **both** client-side and server-side pagination:

### 1. **Server-Side Pagination** (Recommended for Large Datasets)

- ✅ Implemented and ready to use
- ✅ Uses Radzen's `LoadData` event
- ✅ Only loads the current page from the server
- ✅ Efficient for large datasets (thousands+ records)
- ✅ Supports sorting and filtering on the server

### 2. **Client-Side Pagination** (Backward Compatible)

- ✅ Still supported for backward compatibility
- ⚠️ Loads ALL data at once
- ⚠️ Not recommended for large datasets (>1000 records)
- ✅ Works well for small datasets

---

## 📊 How It Works

### Server-Side Pagination Flow

```
User clicks page 2
    ↓
RadzenDataGrid calls LoadData with Skip=10, Take=10
    ↓
SMDataGrid calls LoadItemsPaged(10, 10, orderBy, filter, ct)
    ↓
Service makes API call with pagination parameters
    ↓
Server returns PagedResult<T> with Items and TotalCount
    ↓
Grid displays only the current page
```

### Client-Side Pagination Flow (Current Implementation)

```
Page loads
    ↓
SMDataGrid calls LoadItems(ct)
    ↓
Service loads ALL records from server
    ↓
All data stored in browser memory
    ↓
RadzenDataGrid paginates in browser
```

---

## 🔧 Implementation Guide

### Option 1: Server-Side Pagination (Recommended)

#### Step 1: Update Service Interface

```csharp
// SM_MentalHealthApp.Client/Services/IChatHistoryService.cs
public interface IChatHistoryService
{
    // New method for server-side pagination
    Task<PagedResult<ChatSession>> ListPagedAsync(
        int? patientId,
        int skip,
        int take,
        string? orderBy = null,
        string? filter = null,
        CancellationToken ct = default);

    // Keep existing method for backward compatibility
    Task<IEnumerable<ChatSession>> ListAsync(int? patientId, CancellationToken ct = default);
}
```

#### Step 2: Update Service Implementation

```csharp
// SM_MentalHealthApp.Client/Services/ChatHistoryService.cs
public async Task<PagedResult<ChatSession>> ListPagedAsync(
    int? patientId,
    int skip,
    int take,
    string? orderBy = null,
    string? filter = null,
    CancellationToken ct = default)
{
    AddAuthorizationHeader();

    var url = $"api/chathistory/sessions?skip={skip}&take={take}";
    if (patientId.HasValue)
        url += $"&patientId={patientId.Value}";
    if (!string.IsNullOrEmpty(orderBy))
        url += $"&orderBy={Uri.EscapeDataString(orderBy)}";
    if (!string.IsNullOrEmpty(filter))
        url += $"&filter={Uri.EscapeDataString(filter)}";

    var response = await _http.GetFromJsonAsync<PagedResult<ChatSession>>(url, ct);
    return response ?? new PagedResult<ChatSession> { Items = new(), TotalCount = 0 };
}
```

#### Step 3: Update Controller

```csharp
// SM_MentalHealthApp.Server/Controllers/ChatHistoryController.cs
[HttpGet("sessions")]
public async Task<ActionResult<PagedResult<ChatSession>>> GetUserSessions(
    [FromQuery] int? patientId = null,
    [FromQuery] int skip = 0,
    [FromQuery] int take = 10,
    [FromQuery] string? orderBy = null,
    [FromQuery] string? filter = null)
{
    try
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized("User not authenticated");

        var result = await _chatHistoryService.GetUserSessionsPagedAsync(
            userId.Value,
            patientId,
            skip,
            take,
            orderBy,
            filter);

        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting user sessions");
        return StatusCode(500, "Internal server error");
    }
}
```

#### Step 4: Update Service Layer (Server)

```csharp
// SM_MentalHealthApp.Server/Services/ChatHistoryService.cs
public async Task<PagedResult<ChatSession>> GetUserSessionsPagedAsync(
    int userId,
    int? patientId,
    int skip,
    int take,
    string? orderBy,
    string? filter)
{
    var query = _context.ChatSessions
        .Include(s => s.Patient)
        .Include(s => s.User)
        .AsQueryable();

    // Apply filters
    if (patientId.HasValue)
        query = query.Where(s => s.PatientId == patientId.Value);

    // Apply role-based filtering
    var user = await _context.Users
        .Include(u => u.Role)
        .FirstOrDefaultAsync(u => u.Id == userId);

    if (user?.RoleId == Roles.Patient)
        query = query.Where(s => s.UserId == userId || s.PatientId == userId);
    else if (user?.RoleId == Roles.Doctor)
        query = query.Where(s => s.UserId == userId);
    // Admin sees all

    // Get total count before pagination
    var totalCount = await query.CountAsync();

    // Apply sorting
    if (!string.IsNullOrEmpty(orderBy))
    {
        // Parse orderBy and apply sorting
        // Example: "LastActivityAt desc" or "CreatedAt asc"
        // Implementation depends on your needs
    }

    // Apply pagination
    var items = await query
        .Skip(skip)
        .Take(take)
        .ToListAsync();

    return new PagedResult<ChatSession>
    {
        Items = items,
        TotalCount = totalCount,
        PageNumber = (skip / take) + 1,
        PageSize = take
    };
}
```

#### Step 5: Update Page to Use Server-Side Pagination

```razor
<!-- SM_MentalHealthApp.Client/Pages/ChatHistory.razor -->
<SMDataGrid TItem="ChatSession"
    Columns="_columns"
    Actions="_actions"
    ActionsWidth="120px"
    LoadItemsPaged="LoadChatSessionsPagedAsync"
    @ref="_grid"
    PageSize="10"
    OnRowExpandCallback="@OnRowExpand">
    <!-- ... -->
</SMDataGrid>

@code {
    private async Task<PagedResult<ChatSession>> LoadChatSessionsPagedAsync(
        int skip,
        int take,
        string? orderBy,
        string? filter,
        CancellationToken ct)
    {
        return await ChatHistoryService.ListPagedAsync(selectedPatientId, skip, take, orderBy, filter, ct);
    }
}
```

---

## 📋 Migration Checklist

### For Each Page Using SMDataGrid:

- [ ] **ChatHistory.razor** - Currently uses `LoadItems`, needs migration
- [ ] **ClinicalNotes.razor** - Currently uses `LoadItems`, needs migration
- [ ] **Appointments.razor** - Currently uses `LoadItems`, needs migration
- [ ] **Patients.razor** - Currently uses `LoadItems`, needs migration

### For Each Service:

- [ ] Add `ListPagedAsync` method to interface
- [ ] Implement `ListPagedAsync` in service
- [ ] Update controller to accept pagination parameters
- [ ] Update server-side service to support pagination

---

## ⚠️ Important Notes

### 1. **Backward Compatibility**

- Existing pages using `LoadItems` will continue to work
- They use client-side pagination (loads all data)
- This is fine for small datasets (<1000 records)

### 2. **Performance Considerations**

**Client-Side Pagination:**

- ✅ Fast for small datasets (<100 records)
- ⚠️ Slow initial load for large datasets
- ⚠️ High memory usage
- ⚠️ Network transfer of all data

**Server-Side Pagination:**

- ✅ Fast for any dataset size
- ✅ Low memory usage (only current page)
- ✅ Minimal network transfer
- ✅ Better user experience

### 3. **When to Use Each**

**Use Client-Side Pagination When:**

- Dataset is small (<100 records)
- Data changes frequently and needs real-time updates
- All data is needed for client-side filtering/sorting

**Use Server-Side Pagination When:**

- Dataset is large (>100 records)
- Performance is critical
- Database has proper indexing
- You want to reduce server load

---

## 🚀 Quick Start Example

### Minimal Server-Side Pagination Implementation

```csharp
// 1. Service Interface
Task<PagedResult<ChatSession>> ListPagedAsync(int skip, int take, CancellationToken ct);

// 2. Service Implementation
public async Task<PagedResult<ChatSession>> ListPagedAsync(int skip, int take, CancellationToken ct)
{
    var url = $"api/chathistory/sessions?skip={skip}&take={take}";
    return await _http.GetFromJsonAsync<PagedResult<ChatSession>>(url, ct)
        ?? new PagedResult<ChatSession>();
}

// 3. Controller
[HttpGet("sessions")]
public async Task<ActionResult<PagedResult<ChatSession>>> GetSessions(
    [FromQuery] int skip = 0,
    [FromQuery] int take = 10)
{
    var totalCount = await _context.ChatSessions.CountAsync();
    var items = await _context.ChatSessions
        .Skip(skip)
        .Take(take)
        .ToListAsync();

    return Ok(new PagedResult<ChatSession>
    {
        Items = items,
        TotalCount = totalCount
    });
}

// 4. Page
<SMDataGrid LoadItemsPaged="(skip, take, orderBy, filter, ct) =>
    ChatHistoryService.ListPagedAsync(skip, take, ct)" />
```

---

## 📊 Current Implementation Status

| Page          | Current Method | Status         | Recommendation              |
| ------------- | -------------- | -------------- | --------------------------- |
| ChatHistory   | `LoadItems`    | ⚠️ Client-side | Migrate to `LoadItemsPaged` |
| ClinicalNotes | `LoadItems`    | ⚠️ Client-side | Migrate to `LoadItemsPaged` |
| Appointments  | `LoadItems`    | ⚠️ Client-side | Migrate to `LoadItemsPaged` |
| Patients      | `LoadItems`    | ⚠️ Client-side | Migrate to `LoadItemsPaged` |

---

## ✅ Summary

1. **SMDataGrid now supports server-side pagination** ✅
2. **Backward compatibility maintained** ✅
3. **PagedResult<T> class created** ✅
4. **Ready for migration** ✅

**Next Steps:**

- Migrate pages one by one to use `LoadItemsPaged`
- Update services and controllers to support pagination
- Test with large datasets to verify performance

---

## 🔍 Testing Large Datasets

To test pagination with large datasets:

1. Create test data (1000+ records)
2. Use browser DevTools Network tab
3. Verify only current page is loaded
4. Check response times
5. Monitor memory usage

**Expected Results:**

- Only 10-50 records loaded per page
- Fast response times (<500ms)
- Low memory usage
- Smooth pagination experience
