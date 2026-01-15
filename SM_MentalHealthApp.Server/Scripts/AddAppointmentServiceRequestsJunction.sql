-- =============================================
-- Migration Script: Add Appointment-ServiceRequest Many-to-Many Relationship
-- Description: Creates junction table to link appointments with one or multiple Service Requests
-- Tables: AppointmentServiceRequests
-- =============================================

-- Create AppointmentServiceRequests junction table
CREATE TABLE IF NOT EXISTS `AppointmentServiceRequests` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `AppointmentId` INT NOT NULL COMMENT 'Reference to Appointment',
    `ServiceRequestId` INT NOT NULL COMMENT 'Reference to ServiceRequest',
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT 'When this SR was linked to the appointment',
    `Notes` VARCHAR(500) NULL COMMENT 'Optional notes about why this SR is linked',
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_AppointmentServiceRequests_Appointment_SR` (`AppointmentId`, `ServiceRequestId`),
    CONSTRAINT `FK_AppointmentSR_Appointment` FOREIGN KEY (`AppointmentId`) REFERENCES `Appointments` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_AppointmentSR_ServiceRequest` FOREIGN KEY (`ServiceRequestId`) REFERENCES `ServiceRequests` (`Id`) ON DELETE CASCADE,
    INDEX `IX_AppointmentServiceRequests_AppointmentId` (`AppointmentId`),
    INDEX `IX_AppointmentServiceRequests_ServiceRequestId` (`ServiceRequestId`),
    INDEX `IX_AppointmentServiceRequests_CreatedAt` (`CreatedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Add comments for documentation
ALTER TABLE `AppointmentServiceRequests` COMMENT = 'Junction table linking appointments to one or multiple Service Requests';

-- Migrate existing data: If an Appointment has a ServiceRequestId, create a link in the junction table
-- This preserves existing relationships while enabling the new many-to-many structure
INSERT INTO `AppointmentServiceRequests` (`AppointmentId`, `ServiceRequestId`, `CreatedAt`, `Notes`)
SELECT 
    `Id` AS `AppointmentId`,
    `ServiceRequestId`,
    `CreatedAt`,
    'Migrated from existing ServiceRequestId field' AS `Notes`
FROM `Appointments`
WHERE `ServiceRequestId` IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM `AppointmentServiceRequests` 
      WHERE `AppointmentServiceRequests`.`AppointmentId` = `Appointments`.`Id`
        AND `AppointmentServiceRequests`.`ServiceRequestId` = `Appointments`.`ServiceRequestId`
  );

-- Note: We keep the ServiceRequestId column in Appointments for backward compatibility
-- but new code should use the AppointmentServiceRequests junction table

