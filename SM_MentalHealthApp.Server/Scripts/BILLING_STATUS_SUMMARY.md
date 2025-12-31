# Billing Status System - Implementation Summary

## ✅ What Was Implemented

### 1. Database Schema (Option B - Recommended)

**Migration Script**: `AddBillingStatusAndInvoicing.sql`

**New Columns on ServiceRequestAssignments:**
- `BillingStatus` VARCHAR(20) - Tracks billing lifecycle
- `InvoiceId` BIGINT NULL - Links to invoice
- `BilledAt` DATETIME NULL - When invoiced
- `PaidAt` DATETIME NULL - When paid

**New Tables:**
- `SmeInvoices` - Invoice records
- `SmeInvoiceLines` - Line items with **UNIQUE constraint on AssignmentId** (prevents duplicates)

### 2. Models and Enums

**New File**: `InvoicingModels.cs`
- `BillingStatus` enum (NotBillable, Ready, Invoiced, Paid, Voided)
- `InvoiceStatus` enum (Draft, Sent, Paid, Voided)
- `SmeInvoice` entity
- `SmeInvoiceLine` entity
- Request/Response DTOs

**Updated**: `ServiceRequest.cs`
- Added `BillingStatus`, `InvoiceId`, `BilledAt`, `PaidAt` to `ServiceRequestAssignment`

**Updated**: `BillingModels.cs`
- Added billing status fields to `BillableAssignmentDto`

### 3. Service Layer

**New Service**: `InvoicingService.cs`
- `GenerateInvoiceAsync()` - Creates invoice, updates assignments to Invoiced
- `MarkInvoicePaidAsync()` - Marks invoice and assignments as Paid
- `VoidInvoiceAsync()` - Voids invoice, optionally resets assignments
- `GetInvoicesAsync()` - List invoices with filters
- `GetReadyToBillAssignmentsAsync()` - Preview assignments ready to invoice

**Updated**: `AssignmentLifecycleService.cs`
- Sets `BillingStatus = Ready` when work starts
- Preserves `Invoiced`/`Paid` status during admin overrides
- Sets `BillingStatus = NotBillable` for rejected/abandoned assignments

### 4. API Endpoints

**New Controller**: `InvoicingController.cs`
- `POST /api/Invoicing/generate` - Generate invoice
- `POST /api/Invoicing/{id}/mark-paid` - Mark invoice paid
- `POST /api/Invoicing/{id}/void` - Void invoice
- `GET /api/Invoicing` - List invoices
- `GET /api/Invoicing/{id}` - Get invoice details
- `GET /api/Invoicing/ready-to-bill/{smeUserId}` - Preview ready assignments

**Updated**: `ServiceRequestController.cs`
- Billing query now filters by `BillingStatus = 'Ready' AND InvoiceId IS NULL`
- Returns billing status fields in `BillableAssignmentDto`

### 5. Dependency Injection

**Registered**: `IInvoicingService` → `InvoicingService`

## 🔒 How Re-Billing is Prevented

### Application Level (Query Filter)
```csharp
.Where(a => a.BillingStatus == "Ready" && a.InvoiceId == null)
```
Only `Ready` assignments without an `InvoiceId` appear in billing queries.

### Database Level (Unique Constraint)
```sql
UNIQUE INDEX UQ_InvoiceLine_Assignment (AssignmentId)
```
Even if code tries to add the same assignment twice, the database blocks it.

### Status Preservation
Admin override won't change `BillingStatus` if it's `Invoiced` or `Paid`, preventing accidental reset.

## 📊 Billing Status Flow

```
1. Assignment Created
   → BillingStatus: NotBillable

2. SME Starts Work (Status: InProgress)
   → BillingStatus: Ready ✅ (appears in billing)

3. Generate Invoice
   → BillingStatus: Invoiced
   → InvoiceId: [invoice ID]
   → BilledAt: [timestamp]
   ❌ (no longer appears in billing)

4. Mark Invoice Paid
   → BillingStatus: Paid
   → PaidAt: [timestamp]
   ❌ (permanently excluded from billing)
```

## 🚀 Next Steps

1. **Run Migration**:
   ```bash
   mysql -u root -p customerhealthdb < SM_MentalHealthApp.Server/Scripts/AddBillingStatusAndInvoicing.sql
   ```

2. **Test Invoice Generation**:
   - Use `POST /api/Invoicing/generate` to create invoices
   - Verify assignments move from `Ready` → `Invoiced`
   - Verify they no longer appear in billing queries

3. **Test Payment**:
   - Use `POST /api/Invoicing/{id}/mark-paid` to mark paid
   - Verify assignments move to `Paid`
   - Verify they're permanently excluded

4. **Create UI** (Future):
   - Invoice generation page
   - Invoice list/view page
   - Mark paid / void actions
   - Update billing page to show billing status

## ⚠️ Important Notes

1. **Existing Data**: Migration initializes existing billable assignments to `BillingStatus = 'Ready'`
2. **Backward Compatibility**: Billing queries still check `IsBillable` as fallback
3. **Safety**: Database constraint prevents duplicates even if code has bugs
4. **Audit Trail**: All invoices and line items are tracked with timestamps

## 📝 Answer to Your Question

**Q**: How do I make sure I don't rebill an SR that's already been paid?

**A**: 
1. ✅ Added `BillingStatus` to track billing lifecycle
2. ✅ Only query assignments with `BillingStatus = 'Ready' AND InvoiceId IS NULL`
3. ✅ When generating invoice, set `BillingStatus = 'Invoiced'` and `InvoiceId`
4. ✅ When paid, set `BillingStatus = 'Paid'`
5. ✅ Database unique constraint prevents duplicate billing
6. ✅ Once `Paid`, assignment never appears in billing queries again

**Result**: Once an assignment is invoiced, it disappears from billing. Once paid, it's permanently excluded. The system is safe from re-billing.

