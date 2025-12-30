# Service Request Implementation Guide

## Overview

This implementation adds ServiceRequest-based access control to the application, allowing multiple service requests per client with isolated data visibility between different SMEs.

## Architecture

### Core Concept
- **Access is granted by ServiceRequest assignment, not by Client assignment**
- Each client can have multiple ServiceRequests (SR1, SR2, etc.)
- Each ServiceRequest can be assigned to different SMEs
- Data visibility is isolated per ServiceRequest

### Key Entities

1. **ServiceRequest**: Represents a service request (SR1, SR2, etc.)
   - `ClientId`: The client this SR belongs to
   - `Title`, `Type`, `Status`: Metadata
   - `IsActive`: Soft delete flag

2. **ServiceRequestAssignment**: Links SMEs to ServiceRequests
   - `ServiceRequestId`: The service request
   - `SmeUserId`: The doctor/attorney assigned
   - `IsActive`: Assignment status

3. **Content Entities** (all have nullable `ServiceRequestId`):
   - `ClinicalNote`
   - `ContentItem` (Documents)
   - `JournalEntry`
   - `ChatSession`
   - `Appointment`
   - `ContentAlert`

## Migration Steps

### Phase 1: Add Tables and Columns (Safe - No Behavior Change)

```bash
# Run this SQL script first
mysql -u root -p mentalhealthdb < SM_MentalHealthApp.Server/Scripts/AddServiceRequestTables_Phase1.sql
```

This phase:
- Creates `ServiceRequests` and `ServiceRequestAssignments` tables
- Adds nullable `ServiceRequestId` columns to all content tables
- **Safe to deploy** - application continues to work as before

### Phase 2: Create Default ServiceRequests and Backfill Data

```bash
# Run this SQL script after Phase 1
mysql -u root -p mentalhealthdb < SM_MentalHealthApp.Server/Scripts/AddServiceRequestDataMigration_Phase2.sql
```

This phase:
- Creates a default "General" ServiceRequest for each client with assignments
- Assigns the first active SME to each default SR
- Backfills all existing content to point to the default SR
- **Preserves existing behavior** - everything now operates under default SRs

## Updating Authorization Logic

### The Critical Rule

**Every read/write must pass through ServiceRequestId checks.**

When an SME requests:
- Documents
- Clinical Notes
- Chat Messages
- Appointments
- Journal Entries

You must validate:
1. Is the current SME assigned to the ServiceRequest?
2. If not → 403 Forbidden

### Pattern for Updating Controllers

#### Before (Old Pattern):
```csharp
// Get assigned patient IDs
var assignedPatientIds = await _context.UserAssignments
    .Where(ua => ua.AssignerId == currentUserId.Value && ua.IsActive)
    .Select(ua => ua.AssigneeId)
    .ToListAsync();

// Filter by patient IDs
var notes = await _clinicalNotesService.GetClinicalNotesAsync(patientId, doctorId);
var filteredNotes = notes.Where(n => assignedPatientIds.Contains(n.PatientId)).ToList();
```

#### After (New Pattern):
```csharp
// Get assigned ServiceRequest IDs
var serviceRequestIds = await _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value);

if (!serviceRequestIds.Any())
    return Ok(new List<ClinicalNoteDto>());

// Filter by ServiceRequestId
var query = _context.ClinicalNotes
    .Include(cn => cn.Patient)
    .Include(cn => cn.Doctor)
    .Where(cn => cn.IsActive && 
        cn.ServiceRequestId.HasValue && 
        serviceRequestIds.Contains(cn.ServiceRequestId.Value));

if (patientId.HasValue)
    query = query.Where(cn => cn.PatientId == patientId.Value);

var notes = await query.ToListAsync();
```

### Helper Method Pattern

Add this helper method to your controllers:

```csharp
private async Task<List<int>> GetAssignedServiceRequestIdsAsync(int? smeUserId)
{
    if (!smeUserId.HasValue)
        return new List<int>();
    
    return await _serviceRequestService.GetServiceRequestIdsForSmeAsync(smeUserId.Value);
}

private async Task<bool> CanAccessServiceRequestAsync(int serviceRequestId, int? smeUserId)
{
    if (!smeUserId.HasValue)
        return false;
    
    return await _serviceRequestService.IsSmeAssignedToServiceRequestAsync(serviceRequestId, smeUserId.Value);
}
```

### Controllers to Update

1. **ClinicalNotesController**
   - `GetClinicalNotes` - Filter by ServiceRequestId
   - `GetClinicalNotesPaged` - Filter by ServiceRequestId
   - `CreateClinicalNote` - Set ServiceRequestId (use default if not provided)
   - `UpdateClinicalNote` - Verify access via ServiceRequestId

2. **ContentController** (Documents)
   - `GetContents` - Filter by ServiceRequestId
   - `UploadDocument` - Set ServiceRequestId

3. **ChatController**
   - `GetChatSessions` - Filter by ServiceRequestId
   - `CreateChatSession` - Set ServiceRequestId

4. **AppointmentController**
   - `GetAppointments` - Filter by ServiceRequestId
   - `CreateAppointment` - Set ServiceRequestId

5. **JournalController**
   - `GetJournalEntries` - Filter by ServiceRequestId

### Example: Updated ClinicalNotesController Method

```csharp
[HttpGet]
public async Task<ActionResult<List<ClinicalNoteDto>>> GetClinicalNotes(
    [FromQuery] int? patientId = null,
    [FromQuery] int? doctorId = null,
    [FromQuery] int? serviceRequestId = null) // NEW: Optional SR filter
{
    try
    {
        var currentUserId = GetCurrentUserId();
        var currentRoleId = GetCurrentRoleId();

        // For doctors and attorneys, filter by assigned ServiceRequests
        if ((currentRoleId == Roles.Doctor || currentRoleId == Roles.Attorney) && currentUserId.HasValue)
        {
            var serviceRequestIds = await _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value);
            
            if (!serviceRequestIds.Any())
                return Ok(new List<ClinicalNoteDto>());

            // If specific SR requested, verify access
            if (serviceRequestId.HasValue)
            {
                if (!serviceRequestIds.Contains(serviceRequestId.Value))
                    return Forbid("You are not assigned to this service request");
                
                serviceRequestIds = new List<int> { serviceRequestId.Value };
            }

            // Filter notes by ServiceRequestId
            var query = _context.ClinicalNotes
                .Include(cn => cn.Patient)
                .Include(cn => cn.Doctor)
                .Where(cn => cn.IsActive && 
                    cn.ServiceRequestId.HasValue && 
                    serviceRequestIds.Contains(cn.ServiceRequestId.Value));

            if (patientId.HasValue)
                query = query.Where(cn => cn.PatientId == patientId.Value);

            if (doctorId.HasValue)
                query = query.Where(cn => cn.DoctorId == doctorId.Value);

            var notes = await query
                .OrderByDescending(cn => cn.CreatedAt)
                .ToListAsync();

            return Ok(notes.Select(MapToDto).ToList());
        }

        // For admins, return all notes (or filter by serviceRequestId if provided)
        var allNotes = await _clinicalNotesService.GetClinicalNotesAsync(patientId, doctorId);
        
        if (serviceRequestId.HasValue)
            allNotes = allNotes.Where(n => n.ServiceRequestId == serviceRequestId.Value).ToList();
        
        return Ok(allNotes);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting clinical notes");
        return StatusCode(500, "An error occurred");
    }
}
```

## API Routes

### ServiceRequest Management

- `GET /api/servicerequest` - Get all service requests (filtered by current user's assignments)
- `GET /api/servicerequest/{id}` - Get service request by ID (with access control)
- `POST /api/servicerequest` - Create new service request (Admin/Coordinator only)
- `PUT /api/servicerequest/{id}` - Update service request (Admin/Coordinator only)
- `DELETE /api/servicerequest/{id}` - Delete service request (Admin/Coordinator only)
- `POST /api/servicerequest/{id}/assign` - Assign SME to service request
- `POST /api/servicerequest/{id}/unassign` - Unassign SME from service request
- `GET /api/servicerequest/my-assignments` - Get current SME's service requests
- `GET /api/servicerequest/default/{clientId}` - Get default service request for client

## UI Changes Needed

### SME Dashboard
- Show "My Service Requests" list
- Allow selecting a ServiceRequest to set context
- All tabs (documents, notes, messages) filter by selected SR

### Client Details Page
- Add ServiceRequest selector at top
- Default SR selected automatically
- Switching SR changes what content shows

### Service Request Management (Admin/Coordinator)
- Create new ServiceRequests for clients
- Assign/unassign SMEs to ServiceRequests
- View all ServiceRequests

## Testing Checklist

- [ ] Run Phase 1 migration (tables + columns)
- [ ] Verify application still works (all ServiceRequestIds are NULL)
- [ ] Run Phase 2 migration (create default SRs + backfill)
- [ ] Verify existing data is accessible
- [ ] Test creating new ServiceRequest
- [ ] Test assigning SME to ServiceRequest
- [ ] Test that SME can only see content from assigned SRs
- [ ] Test that SME cannot see content from unassigned SRs
- [ ] Test creating content with ServiceRequestId
- [ ] Test creating content without ServiceRequestId (should use default)

## Rollback Plan

If you need to rollback:

1. **Phase 2 Rollback**: Remove ServiceRequestId values (set to NULL)
   ```sql
   UPDATE ClinicalNotes SET ServiceRequestId = NULL;
   UPDATE Contents SET ServiceRequestId = NULL;
   UPDATE JournalEntries SET ServiceRequestId = NULL;
   UPDATE ChatSessions SET ServiceRequestId = NULL;
   UPDATE Appointments SET ServiceRequestId = NULL;
   UPDATE ContentAlerts SET ServiceRequestId = NULL;
   ```

2. **Phase 1 Rollback**: Drop columns and tables (only if Phase 2 is rolled back first)
   ```sql
   ALTER TABLE ClinicalNotes DROP FOREIGN KEY FK_ClinicalNotes_ServiceRequests_ServiceRequestId;
   ALTER TABLE ClinicalNotes DROP COLUMN ServiceRequestId;
   -- Repeat for all tables...
   DROP TABLE ServiceRequestAssignments;
   DROP TABLE ServiceRequests;
   ```

## Next Steps

1. ✅ Run Phase 1 migration
2. ✅ Deploy application (verify it still works)
3. ✅ Run Phase 2 migration
4. ✅ Update controllers to filter by ServiceRequestId
5. ✅ Update UI to show ServiceRequest selector
6. ✅ Test thoroughly
7. ✅ Enable multi-SR per client functionality

## Support

For questions or issues, refer to:
- `ServiceRequestService.cs` - Service layer implementation
- `ServiceRequestController.cs` - API endpoints
- Migration scripts in `SM_MentalHealthApp.Server/Scripts/`

