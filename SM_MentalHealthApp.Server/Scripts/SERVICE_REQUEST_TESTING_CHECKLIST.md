# Service Request Implementation - Testing Checklist

This document provides a comprehensive testing checklist for the Service Request implementation. Test each scenario to ensure proper authorization and data isolation.

## Prerequisites

- ✅ Phase 1 migration applied (tables + nullable columns)
- ✅ Phase 2 migration applied (default SRs created + data backfilled)
- ✅ All controllers updated with ServiceRequest filtering
- ✅ ChatHistoryService updated to set ServiceRequestId

## Test Environment Setup

Before testing, ensure you have:

1. **Test Users:**
   - At least 2 SME users (doctors/attorneys) - `SME1`, `SME2`
   - At least 2 Client/Patient users - `Client1`, `Client2`
   - 1 Admin user

2. **Test Data:**
   - Client1 has a default ServiceRequest (SR1) assigned to SME1
   - Client1 has a second ServiceRequest (SR2) assigned to SME2
   - Client2 has a default ServiceRequest (SR3) assigned to SME1
   - Some existing content (notes, documents, appointments, journal entries, chat sessions) for Client1

## Test Scenarios

### 1. Service Request Management

#### 1.1 Create Service Request
- [ ] **As Admin/Coordinator:** Create a new ServiceRequest for Client1
  - Verify ServiceRequest is created with correct ClientId
  - Verify ServiceRequest has default status "Active"
  - Verify ServiceRequest has default type "General"

#### 1.2 Assign SME to Service Request
- [ ] **As Admin/Coordinator:** Assign SME1 to SR1
  - Verify assignment is created in ServiceRequestAssignments table
  - Verify `IsActive = true` on assignment
  - Verify `AssignedAt` timestamp is set

#### 1.3 Unassign SME from Service Request
- [ ] **As Admin/Coordinator:** Unassign SME1 from SR1
  - Verify assignment `IsActive = false`
  - Verify SME1 can no longer access SR1 content

#### 1.4 Get Service Requests
- [ ] **As SME1:** Get "My Service Requests"
  - Verify only SR1 and SR3 are returned (assigned to SME1)
  - Verify SR2 is NOT returned
- [ ] **As Admin:** Get all Service Requests
  - Verify all ServiceRequests are returned

#### 1.5 Get Default Service Request
- [ ] **As SME1:** Get default ServiceRequest for Client1
  - Verify default SR (SR1) is returned
  - Verify SME1 has access to it

### 2. Clinical Notes Authorization

#### 2.1 View Clinical Notes (Filtered by ServiceRequest)
- [ ] **As SME1:** Get all clinical notes
  - Verify only notes with `ServiceRequestId` matching SR1 or SR3 are returned
  - Verify notes from SR2 are NOT returned
- [ ] **As SME2:** Get all clinical notes
  - Verify only notes with `ServiceRequestId` matching SR2 are returned
  - Verify notes from SR1 and SR3 are NOT returned

#### 2.2 View Clinical Notes for Specific ServiceRequest
- [ ] **As SME1:** Get clinical notes for SR1
  - Verify only notes with `ServiceRequestId = SR1` are returned
- [ ] **As SME1:** Try to get clinical notes for SR2 (not assigned)
  - Verify 403 Forbid response

#### 2.3 Create Clinical Note
- [ ] **As SME1:** Create a clinical note for Client1
  - Verify note is created with `ServiceRequestId = SR1` (default SR)
  - Verify note is visible to SME1
  - Verify note is NOT visible to SME2

#### 2.4 Get Individual Clinical Note
- [ ] **As SME1:** Get a note from SR1
  - Verify note is returned successfully
- [ ] **As SME2:** Try to get a note from SR1
  - Verify 403 Forbid response

#### 2.5 Search Clinical Notes
- [ ] **As SME1:** Search clinical notes
  - Verify only notes from assigned ServiceRequests are returned
  - Verify notes from unassigned ServiceRequests are excluded

### 3. Content/Documents Authorization

#### 3.1 View Documents (Filtered by ServiceRequest)
- [ ] **As SME1:** Get all documents
  - Verify only documents with `ServiceRequestId` matching assigned SRs are returned
  - Verify documents from unassigned SRs are NOT returned

#### 3.2 View Documents for Specific ServiceRequest
- [ ] **As SME1:** Get documents for SR1
  - Verify only documents with `ServiceRequestId = SR1` are returned
- [ ] **As SME1:** Try to get documents for SR2 (not assigned)
  - Verify 403 Forbid response

#### 3.3 Upload Document
- [ ] **As SME1:** Upload a document for Client1
  - Verify document is created with `ServiceRequestId = SR1` (default SR)
  - Verify document is visible to SME1
  - Verify document is NOT visible to SME2

#### 3.4 Get Individual Document
- [ ] **As SME1:** Get a document from SR1
  - Verify document is returned successfully
- [ ] **As SME2:** Try to get a document from SR1
  - Verify 403 Forbid response

#### 3.5 Toggle Ignore Document
- [ ] **As SME1:** Toggle ignore on a document from SR1
  - Verify operation succeeds
- [ ] **As SME2:** Try to toggle ignore on a document from SR1
  - Verify 403 Forbid response

### 4. Chat Sessions Authorization

#### 4.1 View Chat Sessions (Filtered by ServiceRequest)
- [ ] **As SME1:** Get all chat sessions
  - Verify only sessions with `ServiceRequestId` matching assigned SRs are returned
  - Verify sessions from unassigned SRs are NOT returned

#### 4.2 View Chat Sessions for Specific ServiceRequest
- [ ] **As SME1:** Get chat sessions for SR1
  - Verify only sessions with `ServiceRequestId = SR1` are returned
- [ ] **As SME1:** Try to get chat sessions for SR2 (not assigned)
  - Verify 403 Forbid response

#### 4.3 Create Chat Session
- [ ] **As SME1:** Send a chat message for Client1 (patient mode)
  - Verify new chat session is created with `ServiceRequestId = SR1` (default SR)
  - Verify session is visible to SME1
  - Verify session is NOT visible to SME2

#### 4.4 Generic Mode Chat
- [ ] **As any user:** Send a chat message in generic mode
  - Verify chat session is created with `ServiceRequestId = null`
  - Verify generic mode works correctly

#### 4.5 Get Individual Chat Session
- [ ] **As SME1:** Get a session from SR1
  - Verify session is returned successfully
- [ ] **As SME2:** Try to get a session from SR1
  - Verify 403 Forbid response

### 5. Appointments Authorization

#### 5.1 View Appointments (Filtered by ServiceRequest)
- [ ] **As SME1:** Get all appointments
  - Verify only appointments with `ServiceRequestId` matching assigned SRs are returned
  - Verify appointments from unassigned SRs are NOT returned

#### 5.2 View Appointments for Specific ServiceRequest
- [ ] **As SME1:** Get appointments for SR1
  - Verify only appointments with `ServiceRequestId = SR1` are returned
- [ ] **As SME1:** Try to get appointments for SR2 (not assigned)
  - Verify 403 Forbid response

#### 5.3 Create Appointment
- [ ] **As SME1:** Create an appointment for Client1
  - Verify appointment is created with `ServiceRequestId = SR1` (default SR)
  - Verify appointment is visible to SME1
  - Verify appointment is NOT visible to SME2

#### 5.4 Get Individual Appointment
- [ ] **As SME1:** Get an appointment from SR1
  - Verify appointment is returned successfully
- [ ] **As SME2:** Try to get an appointment from SR1
  - Verify 403 Forbid response

### 6. Journal Entries Authorization

#### 6.1 View Journal Entries (Filtered by ServiceRequest)
- [ ] **As SME1:** Get journal entries for Client1
  - Verify only entries with `ServiceRequestId` matching assigned SRs are returned
  - Verify entries from unassigned SRs are NOT returned

#### 6.2 View Journal Entries for Specific ServiceRequest
- [ ] **As SME1:** Get journal entries for SR1
  - Verify only entries with `ServiceRequestId = SR1` are returned
- [ ] **As SME1:** Try to get journal entries for SR2 (not assigned)
  - Verify 403 Forbid response

#### 6.3 Create Journal Entry (Doctor for Patient)
- [ ] **As SME1:** Create a journal entry for Client1
  - Verify entry is created with `ServiceRequestId = SR1` (default SR)
  - Verify entry is visible to SME1
  - Verify entry is NOT visible to SME2

#### 6.4 Patient Creates Own Journal Entry
- [ ] **As Client1:** Create own journal entry
  - Verify entry is created (ServiceRequestId may be null for patient-created entries)
  - Verify entry is visible to assigned SMEs

#### 6.5 Toggle Ignore Journal Entry
- [ ] **As SME1:** Toggle ignore on an entry from SR1
  - Verify operation succeeds
- [ ] **As SME2:** Try to toggle ignore on an entry from SR1
  - Verify 403 Forbid response

### 7. Multi-ServiceRequest Per Client

#### 7.1 Multiple ServiceRequests for Same Client
- [ ] **As Admin:** Create SR2 for Client1, assign to SME2
  - Verify SR2 is created successfully
  - Verify SME2 can access SR2 content
  - Verify SME1 can still access SR1 content
  - Verify content isolation between SR1 and SR2

#### 7.2 Content Isolation
- [ ] **Setup:** Create content in SR1 and SR2 for Client1
- [ ] **As SME1:** View all content for Client1
  - Verify only SR1 content is visible
- [ ] **As SME2:** View all content for Client1
  - Verify only SR2 content is visible
- [ ] **As Admin:** View all content for Client1
  - Verify both SR1 and SR2 content is visible

### 8. Admin Access

#### 8.1 Admin Sees All Content
- [ ] **As Admin:** Get all clinical notes
  - Verify all notes are returned (no ServiceRequest filtering)
- [ ] **As Admin:** Get all documents
  - Verify all documents are returned
- [ ] **As Admin:** Get all appointments
  - Verify all appointments are returned
- [ ] **As Admin:** Get all chat sessions
  - Verify all chat sessions are returned

#### 8.2 Admin Can Filter by ServiceRequest
- [ ] **As Admin:** Get content with `serviceRequestId` parameter
  - Verify only content from that ServiceRequest is returned
  - Verify filtering works correctly

### 9. Patient Access

#### 9.1 Patient Views Own Content
- [ ] **As Client1:** View own clinical notes
  - Verify only own notes are visible
- [ ] **As Client1:** View own documents
  - Verify only own documents are visible
- [ ] **As Client1:** View own appointments
  - Verify only own appointments are visible

### 10. Edge Cases

#### 10.1 Content Without ServiceRequestId
- [ ] **Scenario:** Content created before Phase 2 migration (ServiceRequestId = NULL)
- [ ] **As SME1:** View content with NULL ServiceRequestId
  - Verify fallback to old assignment check works
  - Verify content is accessible if SME is assigned to patient

#### 10.2 ServiceRequest Deactivated
- [ ] **As Admin:** Deactivate a ServiceRequest
  - Verify assigned SMEs can no longer access its content
  - Verify content still exists in database

#### 10.3 SME Unassigned from ServiceRequest
- [ ] **As Admin:** Unassign SME1 from SR1
  - Verify SME1 can no longer access SR1 content
  - Verify SME1 can still access other assigned ServiceRequests

#### 10.4 Multiple SMEs on Same ServiceRequest
- [ ] **As Admin:** Assign both SME1 and SME2 to SR1
  - Verify both SMEs can access SR1 content
  - Verify content isolation still works for other ServiceRequests

### 11. API Endpoint Testing

#### 11.1 ServiceRequest Endpoints
- [ ] `GET /api/servicerequest` - Returns filtered list
- [ ] `GET /api/servicerequest/{id}` - Access control works
- [ ] `POST /api/servicerequest` - Creates new SR
- [ ] `PUT /api/servicerequest/{id}` - Updates SR
- [ ] `POST /api/servicerequest/{id}/assign` - Assigns SME
- [ ] `POST /api/servicerequest/{id}/unassign` - Unassigns SME
- [ ] `GET /api/servicerequest/my-assignments` - Returns current SME's SRs
- [ ] `GET /api/servicerequest/default/{clientId}` - Returns default SR

#### 11.2 Content Endpoints with ServiceRequestId Parameter
- [ ] All GET endpoints accept optional `serviceRequestId` query parameter
- [ ] Filtering works correctly when parameter is provided
- [ ] Access control enforced when parameter is provided

### 12. Database Verification

#### 12.1 Data Integrity
- [ ] Verify all existing content has ServiceRequestId set (after Phase 2)
- [ ] Verify default ServiceRequests exist for all active clients
- [ ] Verify ServiceRequestAssignments are correct
- [ ] Verify foreign key constraints work correctly

#### 12.2 Query Performance
- [ ] Verify queries with ServiceRequestId filtering are performant
- [ ] Verify indexes are being used (check query execution plans)
- [ ] Verify no N+1 query problems

## Test Execution Log

Use this section to track your test execution:

| Test ID | Test Name | Status | Notes | Date |
|---------|-----------|--------|-------|------|
| 1.1 | Create Service Request | ⬜ | | |
| 1.2 | Assign SME | ⬜ | | |
| ... | ... | ⬜ | | |

**Status Legend:**
- ✅ Pass
- ❌ Fail
- ⬜ Not Tested
- ⚠️ Partial/Needs Review

## Known Issues

Document any issues found during testing:

1. **Issue:** [Description]
   - **Severity:** High/Medium/Low
   - **Steps to Reproduce:** [Steps]
   - **Expected:** [Expected behavior]
   - **Actual:** [Actual behavior]

## Test Results Summary

After completing all tests, provide a summary:

- **Total Tests:** [Number]
- **Passed:** [Number]
- **Failed:** [Number]
- **Not Tested:** [Number]
- **Overall Status:** ✅ Ready for Production / ⚠️ Needs Fixes / ❌ Not Ready

## Next Steps After Testing

1. Fix any issues found
2. Re-test failed scenarios
3. Perform load testing (if applicable)
4. Update documentation
5. Deploy to staging environment
6. Perform UAT (User Acceptance Testing)
7. Deploy to production

