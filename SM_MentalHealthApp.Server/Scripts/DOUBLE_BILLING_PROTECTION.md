# Double Billing Protection System

## Overview

This document describes the comprehensive system implemented to prevent double billing of companies/SMEs for Service Requests, providing legal protection against billing disputes.

## Problem Statement

**Original Issue**: If multiple SMEs from the same company work on the same Service Request, the company could be billed multiple times, creating legal liability.

**Solution**: Implement a charge-based billing system with database-level unique constraints that physically prevent duplicate charges.

## Key Safety Features

### 1. Database-Level Unique Constraint (CRITICAL)

```sql
UNIQUE INDEX `UQ_SR_BillingAccount` (`ServiceRequestId`, `BillingAccountId`)
```

**This constraint ensures**:
- Only ONE charge can exist per Service Request per Billing Account (company or individual)
- Even if code has bugs, the database will reject duplicate charges
- Physically impossible to create duplicate charges

### 2. Charge-Based Billing (Not Assignment-Based)

**Old System**: Billed by assignment → Multiple assignments = Multiple bills

**New System**: Bills by charge → Multiple assignments from same company = ONE charge

### 3. Billing Account Concept

**BillingAccountId**:
- If SME has `CompanyId` → Bills to company
- If SME is independent → Bills to individual SME

**BillingAccountType**: "Company" or "Individual"

### 4. Multiple Layers of Protection

1. **Unique Constraint**: Database prevents duplicates
2. **Status Checks**: Only `Ready` charges with `InvoiceId = NULL` can be invoiced
3. **Transaction Safety**: Charges are created and marked as `Invoiced` in same transaction
4. **Audit Trail**: `InvoiceId` links charges to invoices permanently

## Data Model

### ServiceRequestCharges Table

```sql
CREATE TABLE ServiceRequestCharges (
    Id BIGINT PRIMARY KEY,
    ServiceRequestId INT NOT NULL,
    BillingAccountId INT NOT NULL,  -- CompanyId or SmeUserId
    BillingAccountType VARCHAR(20) NOT NULL,  -- "Company" or "Individual"
    Amount DECIMAL(18, 2) NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Ready',  -- Ready, Invoiced, Paid, Voided
    InvoiceId BIGINT NULL,
    CreatedAt DATETIME NOT NULL,
    InvoicedAt DATETIME NULL,
    PaidAt DATETIME NULL,
    UNIQUE (ServiceRequestId, BillingAccountId)  -- CRITICAL: Prevents duplicates
);
```

### Users Table Addition

```sql
ALTER TABLE Users ADD COLUMN CompanyId INT NULL;
```

If `CompanyId` is set → SME belongs to a company
If `CompanyId` is NULL → SME is independent

## Billing Workflow

### Step 1: SR Becomes Billable

When a Service Request has billable assignments:
1. Get all billable assignments for the SR
2. Group by `BillingAccountId` (CompanyId if exists, else SmeUserId)
3. Create ONE charge per BillingAccountId
4. Database unique constraint prevents duplicates

**Example**:
- SR #55 has 3 assignments:
  - SME A (Company 10) → Completed
  - SME B (Company 10) → Completed
  - SME C (Independent) → Completed
- Result: 2 charges created
  - Charge 1: (SR #55, BillingAccount=Company10)
  - Charge 2: (SR #55, BillingAccount=SME C)

### Step 2: Invoice Generation

1. Query `ServiceRequestCharges` where:
   - `Status = 'Ready'`
   - `InvoiceId IS NULL`
   - `BillingAccountId = X` (for specific company/individual)
   - Date range filter (if provided)

2. Group by `BillingAccountId`

3. Create ONE invoice per BillingAccount

4. Create invoice lines linking to charges (via `ChargeId`)

5. Update charges:
   - `Status = 'Invoiced'`
   - `InvoiceId = invoice.Id`
   - `InvoicedAt = DateTime.UtcNow`

### Step 3: Payment

1. Mark invoice as `Paid`
2. Update all related charges:
   - `Status = 'Paid'`
   - `PaidAt = DateTime.UtcNow`

3. **Result**: These charges will NEVER appear in billing queries again

## Invoice Generation Query (Safe)

```csharp
// Get Ready charges for billing account
var readyCharges = await _context.ServiceRequestCharges
    .Where(c => c.BillingAccountId == billingAccountId &&
                c.BillingAccountType == billingAccountType &&
                c.Status == ChargeStatus.Ready.ToString() &&
                c.InvoiceId == null)  // CRITICAL: Only uninvoiced charges
    .ToListAsync();
```

**This query is safe because**:
- Only selects `Ready` charges
- Only selects charges with `InvoiceId = NULL`
- Once invoiced, `InvoiceId` is set, so charge won't be selected again
- Once paid, `Status = 'Paid'`, so charge won't be selected again

## Protection Against Double Billing

### Scenario 1: Multiple SMEs from Same Company

**SR #100**:
- SME John (Company X) → Completed
- SME Sara (Company X) → Completed

**Result**:
- ONE charge created: (SR #100, BillingAccount=Company X)
- Company X receives ONE invoice with ONE line item
- **Protected by unique constraint**: Cannot create second charge

### Scenario 2: SMEs from Different Companies

**SR #200**:
- SME John (Company X) → Completed
- SME Mike (Company Y) → Completed

**Result**:
- TWO charges created:
  - Charge 1: (SR #200, BillingAccount=Company X)
  - Charge 2: (SR #200, BillingAccount=Company Y)
- Company X receives ONE invoice
- Company Y receives ONE invoice
- **Each company billed separately** (correct behavior)

### Scenario 3: Independent SMEs

**SR #300**:
- SME John (Company X) → Completed
- SME Mike (Independent) → Completed

**Result**:
- TWO charges created:
  - Charge 1: (SR #300, BillingAccount=Company X)
  - Charge 2: (SR #300, BillingAccount=Mike's UserId)
- Company X receives ONE invoice
- Mike receives ONE invoice
- **Each billed separately** (correct behavior)

## Migration Steps

1. **Run Migration Script**: `AddServiceRequestChargesAndCompanyBilling.sql`
   - Adds `CompanyId` to Users
   - Creates `ServiceRequestCharges` table with unique constraint
   - Creates initial charges from existing billable assignments

2. **Update Code**:
   - Add `ServiceRequestCharge` entity
   - Add `ServiceRequestChargeService`
   - Update invoice generation to use charges
   - Register service in DI

3. **Verify**:
   - Check that charges are created correctly
   - Verify unique constraint prevents duplicates
   - Test invoice generation

## Legal Protection

This system provides legal protection because:

1. **Database-Level Guarantee**: Unique constraint physically prevents duplicates
2. **Audit Trail**: Every charge has `InvoiceId` linking it to an invoice
3. **Status Tracking**: Clear status progression (Ready → Invoiced → Paid)
4. **Timestamp Tracking**: `CreatedAt`, `InvoicedAt`, `PaidAt` provide audit trail
5. **No Code-Only Protection**: Even if code has bugs, database enforces rules

## Testing Checklist

- [ ] Create SR with 2 SMEs from same company → Verify ONE charge created
- [ ] Create SR with 2 SMEs from different companies → Verify TWO charges created
- [ ] Generate invoice → Verify charges marked as Invoiced
- [ ] Try to generate invoice again → Verify no charges selected (InvoiceId is set)
- [ ] Mark invoice as paid → Verify charges marked as Paid
- [ ] Try to create duplicate charge → Verify database rejects (unique constraint)

## Summary

**You are protected from double billing because**:

1. ✅ **Unique Constraint**: Database physically prevents duplicate charges
2. ✅ **Charge-Based**: Bills charges, not assignments (groups by company)
3. ✅ **Status Checks**: Only Ready + InvoiceId=NULL charges can be invoiced
4. ✅ **Audit Trail**: Every charge linked to invoice via InvoiceId
5. ✅ **Transaction Safety**: Charges created and marked Invoiced atomically

**Legal Liability**: Minimized because the system is designed to be physically impossible to double-bill the same company for the same SR.

