# Billing Status and Invoicing System Implementation

## Overview

This implementation prevents re-billing of service requests by adding explicit billing status tracking and invoicing tables. Once an assignment is invoiced and paid, it will never appear in billing reports again.

## Core Concept

**The Problem**: Without billing status tracking, you could accidentally bill the same assignment multiple times.

**The Solution**: Add `BillingStatus` to track where each assignment is in the billing lifecycle:
- `NotBillable` - Can't be billed (rejected, cancelled, never started)
- `Ready` - Work done, ready to be invoiced
- `Invoiced` - Included in an invoice
- `Paid` - Invoice has been paid
- `Voided` - Was invoiced, then voided/credited

## Database Changes

### ServiceRequestAssignments Table

New columns:
- `BillingStatus` VARCHAR(20) - Current billing status
- `InvoiceId` BIGINT NULL - Reference to invoice if invoiced
- `BilledAt` DATETIME NULL - When included in invoice
- `PaidAt` DATETIME NULL - When payment received

### New Tables

**SmeInvoices**
- Tracks invoices sent to SMEs
- Includes period, totals, status (Draft, Sent, Paid, Voided)

**SmeInvoiceLines**
- Line items in each invoice
- **CRITICAL**: Unique constraint on `AssignmentId` prevents duplicate billing at database level

## How It Prevents Re-Billing

### 1. Query Filter (Application Level)

When generating invoices or viewing billable assignments, only select:
```sql
WHERE BillingStatus = 'Ready' 
  AND InvoiceId IS NULL
```

This automatically excludes:
- Already invoiced assignments (`BillingStatus = 'Invoiced'`)
- Paid assignments (`BillingStatus = 'Paid'`)
- Non-billable assignments (`BillingStatus = 'NotBillable'`)

### 2. Unique Constraint (Database Level)

```sql
ALTER TABLE SmeInvoiceLines
ADD CONSTRAINT UQ_InvoiceLine_Assignment UNIQUE (AssignmentId);
```

Even if code has a bug, the database will **physically prevent** adding the same assignment to multiple invoices.

## Billing Status Lifecycle

```
Assignment Created
    ↓
Status: Assigned
BillingStatus: NotBillable
    ↓
SME Starts Work
    ↓
Status: InProgress
BillingStatus: Ready  ← Ready to be invoiced
    ↓
Generate Invoice
    ↓
BillingStatus: Invoiced
InvoiceId: [invoice ID]
BilledAt: [timestamp]
    ↓
Mark Invoice Paid
    ↓
BillingStatus: Paid
PaidAt: [timestamp]
```

## Key Rules

1. **Only Ready assignments can be invoiced**
   - Query: `BillingStatus = 'Ready' AND InvoiceId IS NULL`

2. **Once Invoiced, status is locked**
   - Admin override won't change `BillingStatus` if it's `Invoiced` or `Paid`
   - Prevents accidental changes that could cause re-billing

3. **Paid assignments never appear in billing**
   - Billing queries exclude `BillingStatus = 'Paid'`

4. **Voided invoices can reset to Ready**
   - If `ResetAssignmentsToReady = true`, assignments go back to `Ready` for re-invoicing
   - If `false`, they stay `Voided` and are permanently excluded

## API Endpoints

### Generate Invoice
```
POST /api/Invoicing/generate
Body: {
  "smeUserId": 5,
  "periodStart": "2025-12-01",
  "periodEnd": "2025-12-31",
  "taxRate": 0.08,
  "notes": "December 2025 billing"
}
```

**What it does:**
1. Finds all `Ready` assignments for SME in period
2. Creates `SmeInvoice` record
3. Creates `SmeInvoiceLine` for each assignment
4. Updates assignments: `BillingStatus = 'Invoiced'`, sets `InvoiceId`, `BilledAt`
5. **Database constraint prevents duplicates**

### Mark Invoice Paid
```
POST /api/Invoicing/{invoiceId}/mark-paid
Body: {
  "paidDate": "2026-01-15",
  "paymentNotes": "Check #1234"
}
```

**What it does:**
1. Updates invoice: `Status = 'Paid'`, `PaidAt = [date]`
2. Updates all assignments in invoice: `BillingStatus = 'Paid'`, `PaidAt = [date]`
3. **These assignments will never appear in billing again**

### Void Invoice
```
POST /api/Invoicing/{invoiceId}/void
Body: {
  "invoiceId": 123,
  "reason": "Incorrect billing period",
  "resetAssignmentsToReady": true
}
```

**What it does:**
1. Updates invoice: `Status = 'Voided'`, `VoidedAt = [date]`
2. If `resetAssignmentsToReady = true`:
   - Sets assignments back to `BillingStatus = 'Ready'`
   - Clears `InvoiceId` and `BilledAt`
   - Allows re-invoicing
3. If `resetAssignmentsToReady = false`:
   - Sets assignments to `BillingStatus = 'Voided'`
   - Permanently excludes from billing

## Billing Query Updates

### Before (Could Re-Bill)
```csharp
.Where(a => a.IsBillable && 
    (a.Status == "InProgress" || a.Status == "Completed"))
```

### After (Prevents Re-Billing)
```csharp
.Where(a => a.BillingStatus == "Ready" && 
    a.InvoiceId == null &&
    (a.IsBillable || 
     a.Status == "InProgress" || 
     a.Status == "Completed"))
```

## Migration

Run the migration script:
```bash
mysql -u root -p customerhealthdb < SM_MentalHealthApp.Server/Scripts/AddBillingStatusAndInvoicing.sql
```

This will:
1. Add billing status columns to `ServiceRequestAssignments`
2. Create `SmeInvoices` table
3. Create `SmeInvoiceLines` table with unique constraint
4. Initialize existing assignments:
   - Billable → `BillingStatus = 'Ready'`
   - Non-billable → `BillingStatus = 'NotBillable'`

## Testing Scenarios

### Scenario 1: Normal Invoice Flow
1. Assignment completed → `BillingStatus = 'Ready'`
2. Generate invoice → `BillingStatus = 'Invoiced'`, `InvoiceId = 1`
3. Query billable → Assignment **does not appear** (already invoiced)
4. Mark invoice paid → `BillingStatus = 'Paid'`
5. Query billable → Assignment **still does not appear** (already paid)

### Scenario 2: Prevent Duplicate Billing
1. Generate invoice with assignment → `InvoiceId = 1`
2. Try to generate another invoice with same assignment
3. **Result**: Assignment not included (already has `InvoiceId`)
4. Even if code bug tries to add it, **database unique constraint blocks it**

### Scenario 3: Void and Re-Invoice
1. Generate invoice → `BillingStatus = 'Invoiced'`
2. Void invoice with `resetAssignmentsToReady = true`
3. Assignment → `BillingStatus = 'Ready'`, `InvoiceId = null`
4. Can generate new invoice with this assignment

## Safety Features

1. **Application-level**: Query filters prevent Ready assignments from appearing if already invoiced
2. **Database-level**: Unique constraint on `AssignmentId` in `SmeInvoiceLines` prevents duplicates
3. **Status preservation**: Admin override won't change `BillingStatus` if `Invoiced` or `Paid`
4. **Audit trail**: All invoices and line items are tracked with timestamps

## Next Steps

1. ✅ Run migration script
2. ✅ Update billing queries to use `BillingStatus = 'Ready'`
3. ✅ Create invoice generation UI
4. ✅ Create invoice management UI (view, mark paid, void)
5. ✅ Update billing page to show billing status

## Summary

**Question**: How do I prevent re-billing an SR that's already been paid?

**Answer**: 
1. Add `BillingStatus` to track billing lifecycle
2. Only query assignments with `BillingStatus = 'Ready' AND InvoiceId IS NULL`
3. When generating invoice, set `BillingStatus = 'Invoiced'` and `InvoiceId`
4. When paid, set `BillingStatus = 'Paid'`
5. Database unique constraint provides final safety net

**Result**: Once an assignment is invoiced, it will never appear in billing queries again. Once paid, it's permanently excluded.

