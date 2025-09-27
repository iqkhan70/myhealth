# Emergency System API Documentation

## Overview

The Emergency System provides endpoints for receiving emergency messages from registered devices and managing emergency incidents. This system bypasses normal authentication for emergency situations while maintaining security through device token validation.

## Endpoints

### 1. Register Device

**POST** `/api/emergency/register-device`

Registers a device for emergency messaging. Requires patient authentication.

**Request Body:**

```json
{
  "patientId": 1,
  "deviceId": "device_unique_id_123",
  "deviceName": "iPhone 15 Pro",
  "deviceType": "smartphone",
  "deviceModel": "iPhone 15 Pro",
  "operatingSystem": "iOS 17.0"
}
```

**Response:**

```json
{
  "success": true,
  "deviceToken": "encrypted_device_token_here",
  "message": "Device registered successfully",
  "expiresAt": "2025-01-15T10:30:00Z"
}
```

### 2. Receive Emergency Message

**POST** `/api/emergency/receive`

Receives emergency messages from registered devices. Bypasses normal authentication.

**Request Body:**

```json
{
  "deviceToken": "encrypted_device_token_here",
  "emergencyType": "Fall",
  "timestamp": "2024-01-15T10:30:00Z",
  "message": "Patient fell and is unresponsive",
  "severity": "Critical",
  "vitalSigns": {
    "heartRate": 180,
    "bloodPressure": "180/120",
    "temperature": 98.6,
    "oxygenSaturation": 95
  },
  "location": {
    "latitude": 40.7128,
    "longitude": -74.006,
    "accuracy": 10.0,
    "address": "123 Main St, New York, NY",
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "deviceId": "device_unique_id_123"
}
```

**Response:**

```json
{
  "success": true,
  "incidentId": 123,
  "message": "Emergency received and doctors notified"
}
```

### 3. Test Emergency (Development)

**POST** `/api/emergency/test-emergency`

Test endpoint for simulating emergency messages during development.

**Request Body:**

```json
{
  "deviceToken": "test_device_token",
  "emergencyType": "Fall",
  "severity": "Critical",
  "message": "Test emergency - patient fell",
  "deviceId": "test_device_123",
  "heartRate": 180,
  "bloodPressure": "180/120",
  "temperature": 98.6,
  "oxygenSaturation": 95,
  "latitude": 40.7128,
  "longitude": -74.006
}
```

### 4. Get Emergency Incidents

**GET** `/api/emergency/incidents/{doctorId}`

Retrieves emergency incidents for a specific doctor.

**Response:**

```json
[
  {
    "id": 123,
    "patientId": 1,
    "patientName": "John Doe",
    "patientEmail": "john@example.com",
    "emergencyType": "Fall",
    "severity": "Critical",
    "message": "Patient fell and is unresponsive",
    "timestamp": "2024-01-15T10:30:00Z",
    "deviceId": "device_unique_id_123",
    "isAcknowledged": false,
    "vitalSigns": {
      "heartRate": 180,
      "bloodPressure": "180/120"
    },
    "location": {
      "latitude": 40.7128,
      "longitude": -74.006
    }
  }
]
```

### 5. Acknowledge Incident

**POST** `/api/emergency/acknowledge/{incidentId}`

Acknowledges an emergency incident.

**Request Body:**

```json
{
  "doctorId": 2,
  "response": "Contacted patient, they are responsive now",
  "actionTaken": "Called patient, confirmed they are okay"
}
```

## Security Features

### Device Token Validation

- Each device gets a unique, encrypted token
- Tokens expire after 1 year
- Rate limiting: Max 10 messages per 5 minutes per device

### Emergency Types

- `Fall` - Patient fell
- `Cardiac` - Cardiac event
- `PanicAttack` - Panic attack
- `Seizure` - Seizure
- `Overdose` - Overdose
- `SelfHarm` - Self-harm
- `Unconscious` - Unconscious
- `Other` - Other emergency

### Severity Levels

- `Low` - Low priority
- `Medium` - Medium priority
- `High` - High priority
- `Critical` - Critical, immediate attention required

## Testing the System

### 1. Register a Test Device

```bash
curl -X POST "https://your-api.com/api/emergency/register-device" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_PATIENT_TOKEN" \
  -d '{
    "patientId": 1,
    "deviceId": "test_device_123",
    "deviceName": "Test iPhone",
    "deviceType": "smartphone"
  }'
```

### 2. Send Test Emergency

```bash
curl -X POST "https://your-api.com/api/emergency/test-emergency" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceToken": "YOUR_DEVICE_TOKEN",
    "emergencyType": "Fall",
    "severity": "Critical",
    "message": "Test emergency - patient fell",
    "deviceId": "test_device_123",
    "heartRate": 180,
    "latitude": 40.7128,
    "longitude": -74.0060
  }'
```

## Next Steps

1. **Create Database Migration** - Run migration to create emergency tables
2. **Test Endpoints** - Use the test endpoint to verify functionality
3. **Build React Native App** - Create mock app for testing
4. **Add Real-time Notifications** - Implement WebSocket for instant doctor alerts
5. **Add Push Notifications** - Integrate with FCM/APNS for mobile alerts

## Error Handling

The system includes comprehensive error handling:

- Invalid device tokens return 400 Bad Request
- Rate limiting returns 400 Bad Request with "Rate limit exceeded"
- Server errors return 500 Internal Server Error
- All errors are logged for debugging

## Logging

All emergency events are logged with:

- Timestamp
- Device information
- Patient information
- Emergency details
- IP address and user agent
- Response actions taken
