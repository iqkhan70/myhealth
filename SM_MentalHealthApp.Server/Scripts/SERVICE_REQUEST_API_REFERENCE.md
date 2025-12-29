# Service Request API Reference

## Quick Reference

### Table Structure

**ServiceRequest Table:**
- `Id` - Primary key
- `ClientId` - The patient/client this SR belongs to
- `Title` - Service request title
- `Type` - Service request type (e.g., "Medical", "Legal", "General")
- `Status` - Status (e.g., "Active", "Completed", "Cancelled")
- `Description` - Optional description
- `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `IsActive`

**ServiceRequestAssignment Table:**
- `Id` - Primary key
- `ServiceRequestId` - Foreign key to ServiceRequest
- `SmeUserId` - The doctor/attorney assigned to this SR (THIS IS WHERE SME IS STORED)
- `AssignedAt`, `UnassignedAt`, `IsActive`, `AssignedByUserId`

**Important:** `SmeUserId` is NOT in the ServiceRequest table. It's in the ServiceRequestAssignment table because:
- One ServiceRequest can have multiple SME assignments
- This allows for future expansion (multiple SMEs per SR)

## API Endpoints

### Authentication Required
All endpoints require authentication. Include the JWT token in the Authorization header:
```
Authorization: Bearer YOUR_JWT_TOKEN
```

### 1. Get All Service Requests
```
GET /api/ServiceRequest?api-version=1.0
GET /api/ServiceRequest?clientId=123&api-version=1.0
GET /api/ServiceRequest?smeUserId=456&api-version=1.0
```
**Authorization:**
- **SMEs (Doctor/Attorney):** Only see ServiceRequests they're assigned to
- **Admin/Coordinator:** See all ServiceRequests

### 2. Get Service Request by ID
```
GET /api/ServiceRequest/{id}?api-version=1.0
```
**Authorization:**
- **SMEs:** Must be assigned to the ServiceRequest
- **Admin/Coordinator:** Can access any ServiceRequest

### 3. Create Service Request
```
POST /api/ServiceRequest?api-version=1.0
Content-Type: application/json
Authorization: Bearer TOKEN

{
  "clientId": 123,
  "title": "Medical Consultation",
  "type": "Medical",
  "status": "Active",
  "description": "Initial consultation",
  "smeUserId": 456  // Optional: assign SME immediately
}
```
**Authorization:** Admin or Coordinator only

### 4. Update Service Request
```
PUT /api/ServiceRequest/{id}?api-version=1.0
Content-Type: application/json
Authorization: Bearer TOKEN

{
  "title": "Updated Title",
  "type": "Updated Type",
  "status": "Active",
  "description": "Updated description"
}
```
**Authorization:** Admin or Coordinator only

**Example curl:**
```bash
curl -X 'PUT' \
  'https://localhost:5263/api/ServiceRequest/2?api-version=1.0' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer YOUR_JWT_TOKEN' \
  -d '{
  "title": "Troubleshoot",
  "type": "Troubleshoot",
  "status": "Active",
  "description": "Troubleshoot Testing"
}'
```

### 5. Delete Service Request
```
DELETE /api/ServiceRequest/{id}?api-version=1.0
Authorization: Bearer TOKEN
```
**Authorization:** Admin or Coordinator only

### 6. Assign SME to Service Request
```
POST /api/ServiceRequest/{id}/assign?api-version=1.0
Content-Type: application/json
Authorization: Bearer TOKEN

{
  "serviceRequestId": 2,
  "smeUserId": 456
}
```
**Authorization:** Admin or Coordinator only

### 7. Unassign SME from Service Request
```
POST /api/ServiceRequest/{id}/unassign?api-version=1.0
Content-Type: application/json
Authorization: Bearer TOKEN

{
  "serviceRequestId": 2,
  "smeUserId": 456
}
```
**Authorization:** Admin or Coordinator only

### 8. Get My Service Requests (SME only)
```
GET /api/ServiceRequest/my-assignments?api-version=1.0
Authorization: Bearer TOKEN
```
**Authorization:** Doctor or Attorney only
Returns all ServiceRequests assigned to the current user.

### 9. Get Default Service Request for Client
```
GET /api/ServiceRequest/default/{clientId}?api-version=1.0
Authorization: Bearer TOKEN
```
**Authorization:** Any authenticated user

## Common Issues

### 401 Unauthorized
**Cause:** Missing or invalid authentication token

**Solution:**
1. Ensure you're logged in and have a valid JWT token
2. Include the token in the Authorization header:
   ```
   Authorization: Bearer YOUR_JWT_TOKEN
   ```

### 403 Forbidden
**Cause:** User doesn't have the required role

**Solution:**
- For create/update/delete operations, you need Admin or Coordinator role
- For viewing ServiceRequests, SMEs can only see ones they're assigned to

### Where is SmeUserId?
**Question:** "I don't see smeUserId in ServiceRequest table"

**Answer:** `SmeUserId` is in the `ServiceRequestAssignment` table, not `ServiceRequest`. This is by design:
- One ServiceRequest can have multiple SME assignments
- The assignment relationship is stored separately
- To get SMEs for a ServiceRequest, query the `ServiceRequestAssignments` table

**To get SMEs for a ServiceRequest:**
```sql
SELECT sra.SmeUserId, u.FirstName, u.LastName
FROM ServiceRequestAssignments sra
JOIN Users u ON sra.SmeUserId = u.Id
WHERE sra.ServiceRequestId = 2
  AND sra.IsActive = 1;
```

Or use the API - the `ServiceRequestDto` includes an `Assignments` property with all assigned SMEs.

## Testing with curl

### Step 1: Get Authentication Token
First, you need to log in to get a JWT token. The login endpoint depends on your authentication setup.

### Step 2: Use Token in Requests
```bash
# Set your token as a variable
TOKEN="your_jwt_token_here"

# Make authenticated request
curl -X 'PUT' \
  'https://localhost:5263/api/ServiceRequest/2?api-version=1.0' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
  "title": "Troubleshoot",
  "type": "Troubleshoot",
  "status": "Active",
  "description": "Troubleshoot Testing"
}'
```

## Role IDs Reference

Based on your constants:
- **Patient:** RoleId = 1
- **Doctor:** RoleId = 2
- **Admin:** RoleId = 3
- **Coordinator:** RoleId = 4 (if exists)
- **Attorney:** RoleId = 5 (if exists)

Check your `Shared/Constants/Roles.cs` file for exact role IDs.

