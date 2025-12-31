# IsBillable Logic Documentation

## Overview

The `IsBillable` flag on `ServiceRequestAssignments` determines whether an assignment should be included in billing reports and charged to the SME. This flag is automatically managed based on assignment status and workflow.

## When IsBillable is Set

### ✅ IsBillable = TRUE

**Set automatically when:**

1. **Assignment transitions to `InProgress`**
   - Location: `AssignmentLifecycleService.StartAssignmentAsync()`
   - Line: ~146
   - Logic: When SME starts working on an assignment, it becomes billable
   - Code:
     ```csharp
     assignment.Status = AssignmentStatus.InProgress.ToString();
     assignment.StartedAt = DateTime.UtcNow;
     assignment.IsBillable = true; // Mark as billable when work starts
     ```

2. **Assignment transitions to `Completed`**
   - Location: `AssignmentLifecycleService.CompleteAssignmentAsync()`
   - Line: ~181
   - Logic: Completed assignments remain billable
   - Code:
     ```csharp
     assignment.Status = AssignmentStatus.Completed.ToString();
     assignment.CompletedAt = DateTime.UtcNow;
     assignment.IsBillable = true; // Ensure it's billable
     ```

3. **Admin/Coordinator manually sets status to `InProgress` or `Completed`**
   - Location: `AssignmentLifecycleService.UpdateAssignmentStatusAsync()`
   - Line: ~280-281
   - Logic: Any assignment with status InProgress or Completed is billable
   - Code:
     ```csharp
     if (status == AssignmentStatus.InProgress || status == AssignmentStatus.Completed)
         assignment.IsBillable = true;
     ```

### ❌ IsBillable = FALSE

**Set automatically when:**

1. **Assignment is created (initial state)**
   - Location: `ServiceRequestService.AssignSmeToServiceRequestAsync()`
   - Line: ~252
   - Logic: New assignments start as not billable
   - Code:
     ```csharp
     Status = AssignmentStatus.Assigned.ToString(),
     IsBillable = false // Not billable until work starts
     ```

2. **Assignment is rejected**
   - Location: `AssignmentLifecycleService.RejectAssignmentAsync()`
   - Line: ~101
   - Logic: Rejected assignments are never billable
   - Code:
     ```csharp
     assignment.Status = AssignmentStatus.Rejected.ToString();
     assignment.IsBillable = false;
     ```

3. **Assignment is abandoned**
   - Location: `AssignmentLifecycleService.AbandonAssignmentAsync()`
   - Line: ~224
   - Logic: Abandoned assignments are not billable
   - Code:
     ```csharp
     assignment.Status = AssignmentStatus.Abandoned.ToString();
     assignment.IsBillable = false;
     ```

4. **Assignment status is changed to anything other than InProgress or Completed**
   - Location: `AssignmentLifecycleService.UpdateAssignmentStatusAsync()`
   - Line: ~282-283
   - Logic: Only InProgress and Completed are billable
   - Code:
     ```csharp
     else
         assignment.IsBillable = false;
     ```

5. **Admin override changes status away from InProgress/Completed**
   - Location: `AssignmentLifecycleService.AdminOverrideAssignmentStatusAsync()`
   - Line: ~330-333
   - Logic: Follows same rule - only InProgress/Completed are billable
   - Code:
     ```csharp
     if (status == AssignmentStatus.InProgress || status == AssignmentStatus.Completed)
         assignment.IsBillable = true;
     else
         assignment.IsBillable = false;
     ```

## Summary Table

| Status | IsBillable | When Set | Notes |
|--------|------------|----------|-------|
| Assigned | ❌ FALSE | On creation | Not billable until work starts |
| Accepted | ❌ FALSE | On acceptance | Not billable yet - work hasn't started |
| Rejected | ❌ FALSE | On rejection | Never billable |
| InProgress | ✅ TRUE | On start | **Billable** - work has started |
| Completed | ✅ TRUE | On completion | **Billable** - work was done |
| Abandoned | ❌ FALSE | On abandonment | Not billable |

## Key Rules

1. **Work must start**: An assignment is only billable when the SME actually starts working (`InProgress` or `Completed`)
2. **Automatic management**: The flag is automatically set based on status - you don't need to manually set it
3. **Billing queries**: Use `IsBillable = true` AND `Status IN ('InProgress', 'Completed')` to get billable assignments
4. **Admin override**: Admins can change status, and `IsBillable` will automatically update based on the new status

## Billing Query Example

```sql
SELECT 
    sra.Id,
    sr.Title,
    u.FirstName + ' ' + u.LastName AS SmeName,
    sra.Status,
    sra.StartedAt,
    sra.CompletedAt,
    sra.IsBillable
FROM ServiceRequestAssignments sra
INNER JOIN ServiceRequests sr ON sra.ServiceRequestId = sr.Id
INNER JOIN Users u ON sra.SmeUserId = u.Id
WHERE sra.IsBillable = 1
    AND sra.Status IN ('InProgress', 'Completed')
    AND sra.StartedAt >= @startDate
    AND sra.StartedAt <= @endDate;
```

## Important Notes

- **Never manually set IsBillable**: Always let the system manage it based on status
- **Status is source of truth**: The status determines billability, not the flag directly
- **Reversing completion**: If an admin reverses a Completed assignment back to InProgress or earlier, `IsBillable` will automatically update
- **Audit trail**: All status changes (including admin overrides) are logged with warnings

