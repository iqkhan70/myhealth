# Expertise System Implementation

## Overview
This system allows coordinators to quickly find SMEs with matching expertise for Service Requests. It uses a many-to-many relationship between:
- Service Requests ↔ Expertise
- SMEs ↔ Expertise

## Database Schema

### Tables Created
1. **Expertise** - Lookup table for expertise categories
2. **SmeExpertise** - Junction table (SME ↔ Expertise)
3. **ServiceRequestExpertise** - Junction table (ServiceRequest ↔ Expertise)

## Implementation Status

### ✅ Completed
1. Database migration script (`AddExpertiseSystem.sql`)
2. Entity models (`Expertise.cs`)
3. DbContext configuration
4. ExpertiseService for managing expertise

### 🔄 In Progress
1. Update ServiceRequestService to handle expertise on create/update
2. Update GetSmeRecommendationsAsync to filter by expertise and show match counts
3. Create ExpertiseController API
4. Update UI components

### 📋 Remaining
1. Add expertise multi-select to Create/Edit ServiceRequest dialogs
2. Add expertise management UI for Admin
3. Add expertise assignment to SME management page
4. Update AssignSmeDialog to show match counts

## Next Steps

1. Update `ServiceRequestService.CreateServiceRequestAsync` to save expertise
2. Update `ServiceRequestService.UpdateServiceRequestAsync` to update expertise
3. Update `AssignmentLifecycleService.GetSmeRecommendationsAsync` to:
   - Get SR's expertise IDs
   - Filter SMEs by matching expertise
   - Calculate match count
   - Sort by match count, then SME score
4. Create `ExpertiseController` with CRUD endpoints
5. Update UI components

