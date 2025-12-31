# Assignment Lifecycle and SME Scoring Implementation

## Overview

This implementation adds comprehensive assignment lifecycle tracking, SME behavior-based scoring, and billing capabilities to the Service Request system. This enables:

- **Client Retention**: Better service quality through SME scoring and assignment prioritization
- **SME Accountability**: Track assignment outcomes and performance
- **Fair Billing**: Bill SMEs only for work actually performed
- **Data-Driven Decisions**: Coordinators can make informed assignment decisions based on SME scores

## Core Concepts

### 1. Assignment Lifecycle

Assignments go through a clear lifecycle:

```
Assigned → Accepted → InProgress → Completed
              ↓
          Rejected
              ↓
          Abandoned
```

**Status Values:**
- `Assigned`: Initial state when coordinator assigns
- `Accepted`: SME accepted the assignment
- `Rejected`: SME rejected the assignment
- `InProgress`: SME started working on it
- `Completed`: Assignment completed successfully
- `Abandoned`: Assignment abandoned (never completed)

### 2. Outcome Tracking

Each assignment tracks:
- **OutcomeReason**: Why the assignment ended (SME_NoResponse, Client_Cancelled, etc.)
- **ResponsibilityParty**: Who is responsible (SME, Client, System, Coordinator)
- **Timestamps**: AcceptedAt, StartedAt, CompletedAt

### 3. SME Scoring System

Behavior-based scoring (0-150, default 100):

**Negative Events (SME responsible):**
- Reject assignment (no valid reason): -5
- No response within SLA: -10
- Abandoned after acceptance: -15
- Client complaint: -20

**Positive Events:**
- Accepted & completed SR: +3
- Completed within SLA: +5
- Client positive feedback: +10

### 4. Billing Logic

SME is billable ONLY IF:
- Assignment status is `InProgress` or `Completed`
- `IsBillable` flag is set to `true`

NOT billable if:
- Rejected
- Never Accepted
- Client Cancelled before work started

## Database Changes

### ServiceRequestAssignments Table

New columns:
- `Status` VARCHAR(30) - Assignment status
- `OutcomeReason` VARCHAR(50) - Reason for outcome
- `ResponsibilityParty` VARCHAR(30) - Who is responsible
- `AcceptedAt` DATETIME - When SME accepted
- `StartedAt` DATETIME - When SME started work
- `CompletedAt` DATETIME - When assignment completed
- `IsBillable` TINYINT(1) - Whether assignment is billable

### Users Table

New column:
- `SmeScore` INT - Behavior-based score (0-150, default 100)

## Migration

Run the migration script:

```bash
mysql -u root -p customerhealthdb < SM_MentalHealthApp.Server/Scripts/AddAssignmentLifecycleAndSmeScoring.sql
```

This script:
1. Adds all new columns to `ServiceRequestAssignments`
2. Adds `SmeScore` to `Users`
3. Initializes existing assignments with default values
4. Creates necessary indexes

## API Endpoints

### For SMEs (Doctors/Attorneys)

#### Accept Assignment
```
POST /api/servicerequest/assignments/{assignmentId}/accept
Authorization: Bearer {token}
Roles: Doctor, Attorney
```

#### Reject Assignment
```
POST /api/servicerequest/assignments/{assignmentId}/reject
Authorization: Bearer {token}
Roles: Doctor, Attorney
Body: {
  "assignmentId": 123,
  "reason": "SME_Overloaded",
  "notes": "Currently handling 10 active assignments"
}
```

#### Start Assignment
```
POST /api/servicerequest/assignments/{assignmentId}/start
Authorization: Bearer {token}
Roles: Doctor, Attorney
```
**Note**: This sets `IsBillable = true` and marks the assignment as billable.

#### Complete Assignment
```
POST /api/servicerequest/assignments/{assignmentId}/complete
Authorization: Bearer {token}
Roles: Doctor, Attorney
```

#### Get My SME Score
```
GET /api/servicerequest/sme-score
Authorization: Bearer {token}
Roles: Doctor, Attorney
Response: { "score": 105 }
```

### For Coordinators/Admins

#### Get SME Recommendations
```
GET /api/servicerequest/{id}/sme-recommendations?specialization=Cardiology
Authorization: Bearer {token}
Roles: Admin, Coordinator
Response: [
  {
    "smeUserId": 5,
    "smeUserName": "Dr. John Smith",
    "specialization": "Cardiology",
    "smeScore": 120,
    "activeAssignmentsCount": 3,
    "recentRejectionsCount": 0,
    "completionRate": 0.95,
    "recommendationReason": "Excellent score, Low workload, No recent rejections, High completion rate"
  },
  ...
]
```

SMEs are sorted by:
1. Highest score
2. Lowest recent rejections
3. Lowest current workload

#### Update Assignment Status
```
PUT /api/servicerequest/assignments/{assignmentId}/status
Authorization: Bearer {token}
Roles: Admin, Coordinator
Body: {
  "assignmentId": 123,
  "status": "Abandoned",
  "outcomeReason": "Client_NoResponse",
  "responsibilityParty": "Client",
  "notes": "Client did not respond to multiple contact attempts"
}
```

## Usage Workflow

### Coordinator Assigning an SR

1. **Get Recommendations**:
   ```http
   GET /api/servicerequest/123/sme-recommendations
   ```
   Returns SMEs sorted by score, workload, and rejection rate.

2. **Assign to Best SME**:
   ```http
   POST /api/servicerequest/123/assign
   Body: { "serviceRequestId": 123, "smeUserId": 5 }
   ```
   Assignment is created with `Status = "Assigned"`, `IsBillable = false`.

### SME Workflow

1. **SME Receives Assignment** (Status: `Assigned`)
   - Assignment appears in their "My Assignments" list

2. **SME Accepts** (Status: `Assigned` → `Accepted`)
   ```http
   POST /api/servicerequest/assignments/456/accept
   ```
   - Sets `AcceptedAt` timestamp
   - No score change yet

3. **SME Starts Work** (Status: `Accepted` → `InProgress`)
   ```http
   POST /api/servicerequest/assignments/456/start
   ```
   - Sets `StartedAt` timestamp
   - **Sets `IsBillable = true`** (billing starts)
   - No score change yet

4. **SME Completes** (Status: `InProgress` → `Completed`)
   ```http
   POST /api/servicerequest/assignments/456/complete
   ```
   - Sets `CompletedAt` timestamp
   - **Applies +3 score** (accepted & completed)
   - If completed within 7 days of acceptance: **+5 additional score** (within SLA)

### Rejection Workflow

1. **SME Rejects** (Status: `Assigned` → `Rejected`)
   ```http
   POST /api/servicerequest/assignments/456/reject
   Body: {
     "assignmentId": 456,
     "reason": "SME_Overloaded",
     "notes": "Currently handling 15 active assignments"
   }
   ```
   - Sets `OutcomeReason` and `ResponsibilityParty = "SME"`
   - Sets `IsBillable = false`
   - **If reason is NOT legitimate** (not Overloaded, Conflict, or OutOfScope):
     - **Applies -5 score penalty**

### Abandonment Workflow

1. **Coordinator Marks as Abandoned**
   ```http
   PUT /api/servicerequest/assignments/456/status
   Body: {
     "assignmentId": 456,
     "status": "Abandoned",
     "outcomeReason": "SME_NoResponse",
     "responsibilityParty": "SME"
   }
   ```
   - If SME had accepted but then abandoned: **-15 score penalty**

## Billing Queries

### Get Billable Assignments for an SME

```sql
SELECT 
    sra.Id,
    sr.Title,
    sr.ClientId,
    sra.StartedAt,
    sra.CompletedAt,
    sra.Status
FROM ServiceRequestAssignments sra
INNER JOIN ServiceRequests sr ON sra.ServiceRequestId = sr.Id
WHERE sra.SmeUserId = @smeUserId
    AND sra.IsBillable = 1
    AND sra.Status IN ('InProgress', 'Completed')
ORDER BY sra.StartedAt DESC;
```

### Get Billable Assignments for a Period

```sql
SELECT 
    sra.SmeUserId,
    u.FirstName,
    u.LastName,
    COUNT(*) AS BillableAssignments,
    SUM(CASE WHEN sra.Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedCount
FROM ServiceRequestAssignments sra
INNER JOIN Users u ON sra.SmeUserId = u.Id
WHERE sra.IsBillable = 1
    AND sra.StartedAt >= @startDate
    AND sra.StartedAt <= @endDate
GROUP BY sra.SmeUserId, u.FirstName, u.LastName;
```

## Analytics Queries

### SME Performance Dashboard

```sql
SELECT 
    u.Id,
    u.FirstName,
    u.LastName,
    u.SmeScore,
    COUNT(DISTINCT CASE WHEN sra.Status = 'Completed' THEN sra.Id END) AS CompletedCount,
    COUNT(DISTINCT CASE WHEN sra.Status = 'Rejected' THEN sra.Id END) AS RejectedCount,
    COUNT(DISTINCT CASE WHEN sra.Status = 'InProgress' THEN sra.Id END) AS InProgressCount,
    COUNT(DISTINCT CASE WHEN sra.IsBillable = 1 THEN sra.Id END) AS BillableCount,
    AVG(CASE 
        WHEN sra.CompletedAt IS NOT NULL AND sra.AcceptedAt IS NOT NULL 
        THEN DATEDIFF(sra.CompletedAt, sra.AcceptedAt) 
    END) AS AvgDaysToComplete
FROM Users u
LEFT JOIN ServiceRequestAssignments sra ON u.Id = sra.SmeUserId
WHERE u.RoleId IN (2, 4) -- Doctor or Attorney
    AND u.IsActive = 1
GROUP BY u.Id, u.FirstName, u.LastName, u.SmeScore
ORDER BY u.SmeScore DESC;
```

### Assignment Outcome Analysis

```sql
SELECT 
    sra.OutcomeReason,
    sra.ResponsibilityParty,
    COUNT(*) AS Count,
    AVG(u.SmeScore) AS AvgSmeScore
FROM ServiceRequestAssignments sra
LEFT JOIN Users u ON sra.SmeUserId = u.Id
WHERE sra.Status IN ('Rejected', 'Abandoned')
    AND sra.OutcomeReason IS NOT NULL
GROUP BY sra.OutcomeReason, sra.ResponsibilityParty
ORDER BY Count DESC;
```

## Best Practices

### For Coordinators

1. **Always check recommendations** before assigning:
   ```http
   GET /api/servicerequest/{id}/sme-recommendations
   ```

2. **Consider specialization** when filtering:
   ```http
   GET /api/servicerequest/{id}/sme-recommendations?specialization=Cardiology
   ```

3. **Monitor SME scores** regularly to identify performance issues

4. **Set clear expectations** about SLA (default: 7 days from acceptance)

### For SMEs

1. **Accept assignments promptly** to maintain good score

2. **Use legitimate rejection reasons**:
   - `SME_Overloaded`: Too many active assignments
   - `SME_Conflict`: Conflict of interest
   - `SME_OutOfScope`: Outside your expertise

3. **Start work immediately** after accepting to begin billing

4. **Complete within SLA** (7 days) for bonus score points

5. **Monitor your score**:
   ```http
   GET /api/servicerequest/sme-score
   ```

## Score Calculation Examples

### Example 1: Excellent SME

- Started with: 100
- Accepted & completed 10 SRs: +30 (10 × 3)
- Completed 8 within SLA: +40 (8 × 5)
- **Final Score: 170** → Clamped to **150** (max)

### Example 2: Problematic SME

- Started with: 100
- Rejected 5 assignments (no valid reason): -25 (5 × -5)
- Abandoned 2 after acceptance: -30 (2 × -15)
- **Final Score: 45**

### Example 3: Average SME

- Started with: 100
- Accepted & completed 5 SRs: +15 (5 × 3)
- Completed 3 within SLA: +15 (3 × 5)
- Rejected 2 (legitimate reasons): 0 (no penalty)
- **Final Score: 130**

## Integration with Existing Code

### ServiceRequestService

The `AssignSmeToServiceRequestAsync` method now:
- Sets initial `Status = "Assigned"`
- Sets `IsBillable = false`
- Sets `AssignedAt` timestamp

### ServiceRequestDto

The `Assignments` property now includes:
- `Status`
- `OutcomeReason`
- `ResponsibilityParty`
- `AcceptedAt`, `StartedAt`, `CompletedAt`
- `IsBillable`
- `SmeScore`

## Future Enhancements

1. **Client Feedback Loop**: Allow clients to rate completed assignments
2. **SLA Configuration**: Make SLA days configurable per SR type
3. **Automated Reminders**: Notify SMEs of pending assignments
4. **Score History**: Track score changes over time
5. **Performance Reports**: Generate monthly performance reports for SMEs
6. **Coordinator Dashboard**: Visual dashboard showing SME performance metrics

## Support

For questions or issues:
- Review `AssignmentLifecycleService.cs` for implementation details
- Check `ServiceRequestController.cs` for API endpoints
- See migration script: `AddAssignmentLifecycleAndSmeScoring.sql`

